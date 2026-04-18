using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class SceneTransitionService : ISceneTransitionService, ILoadingSceneTargetProvider
    {
        private readonly string _loadingSceneName;
        private readonly string _startupTargetSceneName;
        private string _pendingTargetSceneName;
        private bool _startupTargetConsumed;

        public SceneTransitionService(string loadingSceneName, string startupTargetSceneName)
        {
            _loadingSceneName = loadingSceneName;
            _startupTargetSceneName = startupTargetSceneName;
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

        public bool TryGetTarget(out LoadingSceneTarget target)
        {
            if (!string.IsNullOrWhiteSpace(_pendingTargetSceneName))
            {
                target = new LoadingSceneTarget(_pendingTargetSceneName, false);
                return true;
            }

            if (!_startupTargetConsumed && !string.IsNullOrWhiteSpace(_startupTargetSceneName))
            {
                _startupTargetConsumed = true;
                target = new LoadingSceneTarget(_startupTargetSceneName.Trim(), true);
                return true;
            }

            target = default;
            return false;
        }

        public void ClearPendingTargetScene()
        {
            _pendingTargetSceneName = null;
        }
    }
}
