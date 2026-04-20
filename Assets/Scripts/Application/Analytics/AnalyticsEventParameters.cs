using System.Collections.Generic;
using System.Globalization;

namespace BlossomFlow.Application.Analytics
{
    public sealed class AnalyticsEventParameters
    {
        private readonly Dictionary<string, string> _parameters = new Dictionary<string, string>();

        public AnalyticsEventParameters Add(string key, string value)
        {
            _parameters[key] = value ?? string.Empty;
            return this;
        }

        public AnalyticsEventParameters Add(string key, int value)
        {
            _parameters[key] = value.ToString(CultureInfo.InvariantCulture);
            return this;
        }

        public AnalyticsEventParameters Add(string key, float value)
        {
            _parameters[key] = value.ToString(CultureInfo.InvariantCulture);
            return this;
        }

        public AnalyticsEventParameters Add(string key, bool value)
        {
            _parameters[key] = value ? "true" : "false";
            return this;
        }

        public IReadOnlyDictionary<string, string> Build()
        {
            return new Dictionary<string, string>(_parameters);
        }
    }
}
