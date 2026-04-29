using System;
using BlossomFlow.Application.Events;
using BlossomFlow.MainMenu.Domain;

namespace BlossomFlow.Application.Analytics
{
    public sealed class PlayerRetentionAnalyticsReporter
    {
        private const string FirstOpenDateKey = "blossom_flow.analytics.first_open_date";
        private const string FirstOpenReportedKey = "blossom_flow.analytics.first_open_reported";
        private const string Day1RetentionReportedKey = "blossom_flow.analytics.day_1_retention_reported";
        private const string Day7RetentionReportedKey = "blossom_flow.analytics.day_7_retention_reported";

        private readonly IEventPublisher _eventPublisher;
        private readonly IMainMenuPlayerProgressService _progressService;

        public PlayerRetentionAnalyticsReporter(
            IEventPublisher eventPublisher,
            IMainMenuPlayerProgressService progressService)
        {
            _eventPublisher = eventPublisher;
            _progressService = progressService;
        }

        public void ReportCurrentRetentionMilestone()
        {
            if (_eventPublisher == null)
            {
                return;
            }

            var today = DateTime.UtcNow.Date;
            var firstOpenDate = GetOrCreateFirstOpenDate(today);
            var daysSinceFirstOpen = Math.Max(0, (today - firstOpenDate).Days);

            if (TryReportOnce(FirstOpenReportedKey, "FirstOpen", daysSinceFirstOpen))
            {
                return;
            }

            if (daysSinceFirstOpen >= 7)
            {
                TryReportOnce(Day7RetentionReportedKey, "Day7", daysSinceFirstOpen);
                return;
            }

            if (daysSinceFirstOpen >= 1)
            {
                TryReportOnce(Day1RetentionReportedKey, "Day1", daysSinceFirstOpen);
            }
        }

        private bool TryReportOnce(string reportedKey, string status, int daysSinceFirstOpen)
        {
            if (SafePlayerPrefs.GetInt(reportedKey, 0) == 1)
            {
                return false;
            }

            SafePlayerPrefs.SetInt(reportedKey, 1);
            SafePlayerPrefs.Save();
            PublishRetentionEvent(status, daysSinceFirstOpen);
            return true;
        }

        private DateTime GetOrCreateFirstOpenDate(DateTime fallbackDate)
        {
            var savedDate = SafePlayerPrefs.GetString(FirstOpenDateKey, string.Empty);
            if (DateTime.TryParse(savedDate, out var parsedDate))
            {
                return parsedDate.Date;
            }

            SafePlayerPrefs.SetString(FirstOpenDateKey, fallbackDate.ToString("yyyy-MM-dd"));
            SafePlayerPrefs.Save();
            return fallbackDate;
        }

        private void PublishRetentionEvent(string status, int daysSinceFirstOpen)
        {
            var progress = _progressService != null ? _progressService.Current : MainMenuPlayerProgress.Default;
            var parameters = new AnalyticsEventParameters()
                .Add(RetentionAnalyticsParameterNames.Status, status)
                .Add(RetentionAnalyticsParameterNames.DaysSinceFirstOpen, daysSinceFirstOpen)
                .Add(RetentionAnalyticsParameterNames.PlayerLevel, progress.LevelNumber)
                .Add(RetentionAnalyticsParameterNames.CurrencyBalanceGold, progress.Gold)
                .Add(RetentionAnalyticsParameterNames.CurrencyBalanceMagicalSeed, progress.MagicalSeed);

            _eventPublisher.Publish(new AnalyticsEventRequested(AnalyticsCustomEventNames.PlayerRetention, parameters.Build()));
        }
    }
}
