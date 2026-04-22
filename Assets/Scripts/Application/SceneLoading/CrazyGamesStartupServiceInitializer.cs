using System.Collections;
using BlossomFlow.Application.Platform.CrazyGames;
using CrazyGames;
using UnityEngine;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class CrazyGamesStartupServiceInitializer : IStartupServiceInitializer
    {
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
            CrazySDK.Init(() =>
            {
                Debug.Log("CrazySDK initialized");
                isComplete = true;
            });

            while (!isComplete)
            {
                yield return null;
            }

            _hasInitialized = true;
        }
    }
}
