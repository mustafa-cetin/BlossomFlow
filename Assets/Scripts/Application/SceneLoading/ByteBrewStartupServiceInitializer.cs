using System.Collections;
using BlossomFlow.Application.Analytics;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class ByteBrewStartupServiceInitializer : IStartupServiceInitializer
    {
        private readonly ByteBrewAnalyticsEventService _analyticsEventService;
        private readonly PlayerRetentionAnalyticsReporter _retentionAnalyticsReporter;
        private bool _hasInitialized;

        public ByteBrewStartupServiceInitializer(
            ByteBrewAnalyticsEventService analyticsEventService,
            PlayerRetentionAnalyticsReporter retentionAnalyticsReporter)
        {
            _analyticsEventService = analyticsEventService;
            _retentionAnalyticsReporter = retentionAnalyticsReporter;
        }

        public IEnumerator InitializeIfNeeded()
        {
            if (_hasInitialized)
            {
                yield break;
            }

            _hasInitialized = true;
            _analyticsEventService?.InitializeIfNeeded();
            _retentionAnalyticsReporter?.ReportCurrentRetentionMilestone();
        }
    }
}
