using System.Collections.Generic;

namespace BlossomFlow.Application.Analytics
{
    public readonly struct AnalyticsEventRequested
    {
        public AnalyticsEventRequested(string eventName, IReadOnlyDictionary<string, string> parameters = null)
        {
            EventName = eventName;
            Parameters = parameters;
        }

        public string EventName { get; }
        public IReadOnlyDictionary<string, string> Parameters { get; }
    }
}
