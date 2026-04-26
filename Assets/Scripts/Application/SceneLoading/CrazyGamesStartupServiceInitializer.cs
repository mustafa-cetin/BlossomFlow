using System.Collections;
using BlossomFlow.Application.Platform.CrazyGames;
using CrazyGames;
using UnityEngine;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class CrazyGamesStartupServiceInitializer : IStartupServiceInitializer
    {
        private const float InitializationTimeoutSeconds = 10f;

        private readonly CrazyGamesSdkConfiguration _configuration;
        private bool _hasInitialized;

        public CrazyGamesStartupServiceInitializer(CrazyGamesSdkConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IEnumerator InitializeIfNeeded()
        {
            if (_hasInitialized ||
                _configuration == null ||
                !_configuration.IsEnabled ||
                CrazySDK.IsInitialized ||
                !CrazySDK.IsAvailable)
            {
                _hasInitialized = true;
                yield break;
            }

            var isComplete = false;
            var startTime = Time.realtimeSinceStartup;

            try
            {
                CrazySDK.Init(() =>
                {
                    Debug.Log("CrazySDK initialized.");
                    isComplete = true;
                });
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"CrazySDK initialization failed. Falling back to PlayerPrefs. {exception.Message}");
                CrazyGamesSdkRuntime.Configure(false);
                _hasInitialized = true;
                yield break;
            }

            while (!isComplete && !CrazySDK.IsInitialized)
            {
                if (Time.realtimeSinceStartup - startTime >= InitializationTimeoutSeconds)
                {
                    Debug.LogWarning("CrazySDK initialization timed out. Falling back to PlayerPrefs.");
                    CrazyGamesSdkRuntime.Configure(false);
                    _hasInitialized = true;
                    yield break;
                }

                yield return null;
            }

            _hasInitialized = true;
        }
    }
}
