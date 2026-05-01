using System;
using BlossomFlow.Application.Events;
using BlossomFlow.Application.Levels;

namespace BlossomFlow.MainMenu.Domain
{
    public sealed class MainMenuPlayController : IDisposable
    {
        private readonly ILevelSceneLoader _levelSceneLoader;
        private readonly ILevelSelectionWriter _levelSelectionWriter;
        private readonly IDisposable _playRequestedSubscription;

        public MainMenuPlayController(
            IEventSubscriber eventSubscriber,
            ILevelSceneLoader levelSceneLoader,
            ILevelSelectionWriter levelSelectionWriter)
        {
            _levelSceneLoader = levelSceneLoader;
            _levelSelectionWriter = levelSelectionWriter;
            _playRequestedSubscription = eventSubscriber.Subscribe<MainMenuPlayRequested>(OnPlayRequested);
        }

        public void Dispose()
        {
            _playRequestedSubscription.Dispose();
        }

        private void OnPlayRequested(MainMenuPlayRequested playRequested)
        {
            _levelSelectionWriter.SelectLevel(playRequested.LevelNumber);
            _levelSceneLoader.LoadLevel(playRequested.LevelNumber);
        }
    }
}
