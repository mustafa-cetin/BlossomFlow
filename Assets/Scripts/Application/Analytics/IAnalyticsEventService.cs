using System.Collections.Generic;

namespace BlossomFlow.Application.Analytics
{
    public interface IAnalyticsEventService
    {
        bool TrackCustomEvent(string eventName);
        bool TrackCustomEvent(string eventName, IReadOnlyDictionary<string, string> parameters);
    }
}
