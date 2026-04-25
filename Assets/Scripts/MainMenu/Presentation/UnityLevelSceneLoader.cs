using BlossomFlow.Application.SceneLoading;
using BlossomFlow.MainMenu.Domain;
using UnityEngine;

namespace BlossomFlow.MainMenu.Presentation
{
    public sealed class UnityLevelSceneLoader : ILevelSceneLoader
    {
        private readonly string _levelSceneName;
        private readonly ISceneTransitionService _sceneTransitionService;

        public UnityLevelSceneLoader(string levelSceneName, ISceneTransitionService sceneTransitionService)
        {
            _levelSceneName = levelSceneName;
            _sceneTransitionService = sceneTransitionService;
        }

        public void LoadLevel(int levelNumber)
        {
            _ = levelNumber;

            if (string.IsNullOrWhiteSpace(_levelSceneName))
            {
                Debug.LogError("Level scene name is empty. Set it on MainMenuInstaller before using the Play button.");
                return;
            }

            _sceneTransitionService.LoadScene(_levelSceneName);
        }
    }
}
