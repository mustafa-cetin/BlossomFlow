using System.Collections.Generic;

namespace BlossomFlow.Application.Analytics
{
    public sealed class ByteBrewAnalyticsEventService : IAnalyticsEventService
    {
        private readonly ByteBrewAnalyticsConfiguration _configuration;
        private bool _hasInitializationBeenRequested;

        public ByteBrewAnalyticsEventService(ByteBrewAnalyticsConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool TrackCustomEvent(string eventName)
        {
            return false;
        }

        public bool TrackCustomEvent(string eventName, IReadOnlyDictionary<string, string> parameters)
        {
            return false;
        }

        public void InitializeIfNeeded()
        {
            if (_hasInitializationBeenRequested || _configuration == null || !_configuration.IsEnabled)
            {
                return;
            }

            _hasInitializationBeenRequested = true;
        }
    }
}
