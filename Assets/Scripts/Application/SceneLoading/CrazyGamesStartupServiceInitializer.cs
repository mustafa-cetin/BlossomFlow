using System.Collections;
using CrazyGames;
using UnityEngine;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class CrazyGamesStartupServiceInitializer : IStartupServiceInitializer
    {
        private bool _hasInitialized;

        public IEnumerator InitializeIfNeeded()
        {
            if (_hasInitialized || CrazySDK.IsInitialized || !CrazySDK.IsAvailable)
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
