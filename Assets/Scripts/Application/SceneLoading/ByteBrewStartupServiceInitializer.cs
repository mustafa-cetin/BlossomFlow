using System.Collections;
using BlossomFlow.Application.Analytics;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class ByteBrewStartupServiceInitializer : IStartupServiceInitializer
    {
        private readonly ByteBrewAnalyticsEventService _analyticsEventService;
        private readonly PlayerRetentionAnalyticsReporter _retentionAnalyticsReporter;
        private readonly PlayerJourneyAnalyticsReporter _journeyAnalyticsReporter;
        private bool _hasInitialized;

        public ByteBrewStartupServiceInitializer(
            ByteBrewAnalyticsEventService analyticsEventService,
            PlayerRetentionAnalyticsReporter retentionAnalyticsReporter,
            PlayerJourneyAnalyticsReporter journeyAnalyticsReporter)
        {
            _analyticsEventService = analyticsEventService;
            _retentionAnalyticsReporter = retentionAnalyticsReporter;
            _journeyAnalyticsReporter = journeyAnalyticsReporter;
        }

        public IEnumerator InitializeIfNeeded()
        {
            if (_hasInitialized)
            {
                yield break;
            }

            _hasInitialized = true;
            _analyticsEventService?.InitializeIfNeeded();
            _journeyAnalyticsReporter?.ReportAppStarted();
            _retentionAnalyticsReporter?.ReportCurrentRetentionMilestone();
        }
    }
}
