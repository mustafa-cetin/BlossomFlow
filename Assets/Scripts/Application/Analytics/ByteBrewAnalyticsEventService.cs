using System;
using System.Collections.Generic;
using ByteBrewSDK;
using UnityEngine;

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
            if (!CanTrack(eventName))
            {
                return false;
            }

            try
            {
                ByteBrew.NewCustomEvent(eventName);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"ByteBrew custom event failed: {eventName}. {exception.Message}");
                return false;
            }
        }

        public bool TrackCustomEvent(string eventName, IReadOnlyDictionary<string, string> parameters)
        {
            if (!CanTrack(eventName))
            {
                return false;
            }

            try
            {
                ByteBrew.NewCustomEvent(eventName, CopyValidParameters(parameters));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"ByteBrew custom event failed: {eventName}. {exception.Message}");
                return false;
            }
        }

        public void InitializeIfNeeded()
        {
            if (_hasInitializationBeenRequested || _configuration == null || !_configuration.IsEnabled)
            {
                return;
            }

            _hasInitializationBeenRequested = true;
            EnsureByteBrewInstance();

            try
            {
                ByteBrew.InitializeByteBrew();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"ByteBrew initialization failed. Analytics events will be skipped. {exception.Message}");
            }
        }

        private bool CanTrack(string eventName)
        {
            if (_configuration == null || !_configuration.IsEnabled || !AnalyticsEventValidator.IsValidName(eventName))
            {
                return false;
            }

            InitializeIfNeeded();
            return ByteBrew.IsInitilized;
        }

        private static Dictionary<string, string> CopyValidParameters(IReadOnlyDictionary<string, string> parameters)
        {
            var copiedParameters = new Dictionary<string, string>();
            if (parameters == null)
            {
                return copiedParameters;
            }

            foreach (var parameter in parameters)
            {
                if (!AnalyticsEventValidator.IsValidName(parameter.Key))
                {
                    continue;
                }

                copiedParameters[parameter.Key] = parameter.Value ?? string.Empty;
            }

            return copiedParameters;
        }

        private static void EnsureByteBrewInstance()
        {
            if (ByteBrew.Instance != null)
            {
                return;
            }

            var byteBrewObject = new GameObject("ByteBrew");
            byteBrewObject.AddComponent<ByteBrew>();
        }
    }
}
