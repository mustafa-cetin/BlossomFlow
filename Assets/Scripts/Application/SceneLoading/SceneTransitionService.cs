using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class SceneTransitionService : ISceneTransitionService, ILoadingSceneTargetProvider
    {
        private readonly string _loadingSceneName;
        private string _pendingTargetSceneName;

        public SceneTransitionService(string loadingSceneName)
        {
            _loadingSceneName = loadingSceneName;
        }

        public void LoadSceneThroughLoading(string targetSceneName)
        {
            var normalizedTargetSceneName = targetSceneName?.Trim();
            var normalizedLoadingSceneName = _loadingSceneName?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedTargetSceneName))
            {
                Debug.LogError("Target scene name is empty. Set the scene name before requesting a loading transition.");
                return;
            }

            if (string.IsNullOrWhiteSpace(normalizedLoadingSceneName))
            {
                Debug.LogError("Loading scene name is empty. Set it on RootInstaller before requesting scene transitions.");
                return;
            }

            if (string.Equals(normalizedTargetSceneName, normalizedLoadingSceneName, StringComparison.Ordinal))
            {
                Debug.LogError("Target scene cannot be the loading scene.");
                return;
            }

            _pendingTargetSceneName = normalizedTargetSceneName;
            SceneManager.LoadScene(normalizedLoadingSceneName);
        }

        public bool TryGetPendingTargetScene(out string targetSceneName)
        {
            targetSceneName = _pendingTargetSceneName;
            return !string.IsNullOrWhiteSpace(targetSceneName);
        }

        public void ClearPendingTargetScene()
        {
            _pendingTargetSceneName = null;
        }
    }
}
