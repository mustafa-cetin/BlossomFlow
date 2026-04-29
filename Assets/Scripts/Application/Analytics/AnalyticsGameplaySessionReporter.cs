using BlossomFlow.Application.Events;
using BlossomFlow.Application.Platform.CrazyGames;
using BlossomFlow.MainMenu.Domain;
using UnityEngine;

namespace BlossomFlow.Application.Analytics
{
    public sealed class AnalyticsGameplaySessionReporter : IGameplayPlatformSessionReporter
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly IMainMenuPlayerProgressService _progressService;
        private bool _isGameplayActive;
        private float _startedAtSeconds;

        public AnalyticsGameplaySessionReporter(
            IEventPublisher eventPublisher,
            IMainMenuPlayerProgressService progressService)
        {
            _eventPublisher = eventPublisher;
            _progressService = progressService;
        }

        public void NotifyGameplayStart()
        {
            if (_isGameplayActive)
            {
                return;
            }

            _isGameplayActive = true;
            _startedAtSeconds = Time.realtimeSinceStartup;
            PublishGameplayEvent("Start", 0);
        }

        public void NotifyGameplayStop()
        {
            if (!_isGameplayActive)
            {
                return;
            }

            _isGameplayActive = false;
            var durationSeconds = Mathf.Max(0, Mathf.RoundToInt(Time.realtimeSinceStartup - _startedAtSeconds));
            PublishGameplayEvent("Stop", durationSeconds);
        }

        private void PublishGameplayEvent(string status, int durationSeconds)
        {
            if (_eventPublisher == null)
            {
                return;
            }

            var progress = _progressService != null ? _progressService.Current : MainMenuPlayerProgress.Default;
            var parameters = new AnalyticsEventParameters()
                .Add(GameplayAnalyticsParameterNames.Status, status)
                .Add(GameplayAnalyticsParameterNames.LevelID, progress.LevelNumber)
                .Add(GameplayAnalyticsParameterNames.PlayerLevel, progress.LevelNumber)
                .Add(GameplayAnalyticsParameterNames.CurrencyBalanceGold, progress.Gold)
                .Add(GameplayAnalyticsParameterNames.CurrencyBalanceMagicalSeed, progress.MagicalSeed);

            if (durationSeconds > 0)
            {
                parameters.Add(GameplayAnalyticsParameterNames.DurationSeconds, durationSeconds);
            }

            _eventPublisher.Publish(new AnalyticsEventRequested(AnalyticsCustomEventNames.Gameplay, parameters.Build()));
        }
    }
}
