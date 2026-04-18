using System;
using BlossomFlow.Application.Events;

namespace BlossomFlow.MainMenu.Domain
{
    public sealed class MainMenuPlayController : IDisposable
    {
        private readonly ILevelSceneLoader _levelSceneLoader;
        private readonly IDisposable _playRequestedSubscription;

        public MainMenuPlayController(IEventSubscriber eventSubscriber, ILevelSceneLoader levelSceneLoader)
        {
            _levelSceneLoader = levelSceneLoader;
            _playRequestedSubscription = eventSubscriber.Subscribe<MainMenuPlayRequested>(OnPlayRequested);
        }

        public void Dispose()
        {
            _playRequestedSubscription.Dispose();
        }

        private void OnPlayRequested(MainMenuPlayRequested playRequested)
        {
            _levelSceneLoader.LoadLevel(playRequested.LevelNumber);
        }
    }
}
