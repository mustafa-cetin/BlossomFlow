using System.Collections.Generic;
using ByteBrewSDK;
using UnityEngine;

namespace BlossomFlow.Application.Analytics
{
    public sealed class ByteBrewAnalyticsEventService : IAnalyticsEventService
    {
        public bool TrackCustomEvent(string eventName)
        {
            if (!AnalyticsEventValidator.IsValidName(eventName))
            {
                LogInvalidEventName(eventName);
                return false;
            }

            ByteBrew.NewCustomEvent(eventName);
            return true;
        }

        public bool TrackCustomEvent(string eventName, IReadOnlyDictionary<string, string> parameters)
        {
            if (!AnalyticsEventValidator.IsValidName(eventName))
            {
                LogInvalidEventName(eventName);
                return false;
            }

            if (!TryCopyParameters(parameters, out var byteBrewParameters))
            {
                return false;
            }

            ByteBrew.NewCustomEvent(eventName, byteBrewParameters);
            return true;
        }

        private static bool TryCopyParameters(
            IReadOnlyDictionary<string, string> parameters,
            out Dictionary<string, string> byteBrewParameters)
        {
            byteBrewParameters = new Dictionary<string, string>();

            if (parameters == null || parameters.Count == 0)
            {
                return true;
            }

            foreach (var parameter in parameters)
            {
                if (!AnalyticsEventValidator.IsValidName(parameter.Key))
                {
                    Debug.LogWarning($"ByteBrew custom event parameter key is invalid and the event was not sent: '{parameter.Key}'. Use only letters, numbers, and underscores.");
                    return false;
                }

                byteBrewParameters[parameter.Key] = parameter.Value ?? string.Empty;
            }

            return true;
        }

        private static void LogInvalidEventName(string eventName)
        {
            Debug.LogWarning($"ByteBrew custom event name is invalid and was not sent: '{eventName}'. Use only letters, numbers, and underscores.");
        }
    }
}
