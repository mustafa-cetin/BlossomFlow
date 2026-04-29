using System;
using BlossomFlow.Application.Analytics;
using BlossomFlow.Application.Events;
using BlossomFlow.Application.Levels;

namespace BlossomFlow.MainMenu.Domain
{
    public sealed class MainMenuPlayController : IDisposable
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly ILevelSceneLoader _levelSceneLoader;
        private readonly ILevelSelectionWriter _levelSelectionWriter;
        private readonly IMainMenuPlayerProgressService _progressService;
        private readonly IDisposable _playRequestedSubscription;

        public MainMenuPlayController(
            IEventPublisher eventPublisher,
            IEventSubscriber eventSubscriber,
            ILevelSceneLoader levelSceneLoader,
            ILevelSelectionWriter levelSelectionWriter,
            IMainMenuPlayerProgressService progressService)
        {
            _eventPublisher = eventPublisher;
            _levelSceneLoader = levelSceneLoader;
            _levelSelectionWriter = levelSelectionWriter;
            _progressService = progressService;
            _playRequestedSubscription = eventSubscriber.Subscribe<MainMenuPlayRequested>(OnPlayRequested);
        }

        public void Dispose()
        {
            _playRequestedSubscription.Dispose();
        }

        private void OnPlayRequested(MainMenuPlayRequested playRequested)
        {
            PublishGameplayRequested(playRequested.LevelNumber);
            _levelSelectionWriter.SelectLevel(playRequested.LevelNumber);
            _levelSceneLoader.LoadLevel(playRequested.LevelNumber);
        }

        private void PublishGameplayRequested(int levelNumber)
        {
            if (_eventPublisher == null)
            {
                return;
            }

            var progress = _progressService != null ? _progressService.Current : MainMenuPlayerProgress.Default;
            var parameters = new AnalyticsEventParameters()
                .Add(GameplayAnalyticsParameterNames.Status, "Requested")
                .Add(GameplayAnalyticsParameterNames.LevelID, levelNumber)
                .Add(GameplayAnalyticsParameterNames.PlayerLevel, progress.LevelNumber)
                .Add(GameplayAnalyticsParameterNames.CurrencyBalanceGold, progress.Gold)
                .Add(GameplayAnalyticsParameterNames.CurrencyBalanceMagicalSeed, progress.MagicalSeed);

            _eventPublisher.Publish(new AnalyticsEventRequested(AnalyticsCustomEventNames.Gameplay, parameters.Build()));
        }
    }
}
