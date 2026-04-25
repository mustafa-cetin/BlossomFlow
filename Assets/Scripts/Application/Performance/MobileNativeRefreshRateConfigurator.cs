using UnityEngine;

namespace BlossomFlow.Application.Performance
{
    public sealed class MobileNativeRefreshRateConfigurator : IFrameRateConfigurator
    {
        private const int FallbackRefreshRate = 60;
        private bool _hasApplied;

        public void Apply()
        {
            if (_hasApplied)
            {
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            var nativeRefreshRate = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
            if (nativeRefreshRate <= 0)
            {
                nativeRefreshRate = FallbackRefreshRate;
            }

            UnityEngine.Application.targetFrameRate = nativeRefreshRate;
            Debug.Log($"Native mobile refresh rate detected as {nativeRefreshRate} Hz. UnityEngine.Application.targetFrameRate set to {UnityEngine.Application.targetFrameRate}.");
#endif

            _hasApplied = true;
        }
    }
}
