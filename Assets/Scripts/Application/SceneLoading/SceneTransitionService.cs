using UnityEngine;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class SceneTransitionService : ISceneTransitionService
    {
        private readonly ISceneLoader _sceneLoader;

        public SceneTransitionService(ISceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public void LoadScene(string targetSceneName)
        {
            var normalizedTargetSceneName = targetSceneName?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedTargetSceneName))
            {
                Debug.LogError("Target scene name is empty. Set the scene name before requesting a scene transition.");
                return;
            }

            if (_sceneLoader == null)
            {
                Debug.LogError("Scene loader is not available.");
                return;
            }

            _sceneLoader.Load(normalizedTargetSceneName);
        }
    }
}
