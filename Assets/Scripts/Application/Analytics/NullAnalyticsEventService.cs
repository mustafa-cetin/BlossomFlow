using System.Collections.Generic;

namespace BlossomFlow.Application.Analytics
{
    public sealed class NullAnalyticsEventService : IAnalyticsEventService
    {
        public bool TrackCustomEvent(string eventName)
        {
            return false;
        }

        public bool TrackCustomEvent(string eventName, IReadOnlyDictionary<string, string> parameters)
        {
            return false;
        }
    }
}
