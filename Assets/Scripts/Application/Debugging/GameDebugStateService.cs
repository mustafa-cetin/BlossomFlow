#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using BlossomFlow.Application.Events;
using BlossomFlow.Application.Levels;
using BlossomFlow.MainMenu.Domain;

namespace BlossomFlow.Application.Debugging
{
    public sealed class GameDebugStateService : IGameDebugStateService
    {
        private readonly IMainMenuPlayerProgressService _progressService;
        private readonly ILevelSelectionWriter _levelSelectionWriter;
        private readonly ICurrentLevelDefinitionProvider _currentLevelProvider;
        private readonly IEventPublisher _eventPublisher;

        public GameDebugStateService(
            IMainMenuPlayerProgressService progressService,
            ILevelSelectionWriter levelSelectionWriter,
            ICurrentLevelDefinitionProvider currentLevelProvider,
            IEventPublisher eventPublisher)
        {
            _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
            _levelSelectionWriter = levelSelectionWriter ?? throw new ArgumentNullException(nameof(levelSelectionWriter));
            _currentLevelProvider = currentLevelProvider ?? throw new ArgumentNullException(nameof(currentLevelProvider));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public GameDebugSnapshot Current
        {
            get
            {
                var progress = _progressService.Current;
                return new GameDebugSnapshot(
                    progress.LevelNumber,
                    _currentLevelProvider.CurrentLevelNumber,
                    0,
                    progress.CurrentLives,
                    progress.MaxLives,
                    progress.Gold,
                    progress.MagicalSeed,
                    progress.NextLifeCountdown);
            }
        }

        public void SetLevel(int levelNumber, bool reloadCurrentLevel)
        {
            var nextLevelNumber = NormalizeLevel(levelNumber);
            var progress = _progressService.Current;
            _levelSelectionWriter.SelectLevel(nextLevelNumber);
            _progressService.SetProgress(new MainMenuPlayerProgress(
                nextLevelNumber,
                progress.CurrentLives,
                progress.MaxLives,
                progress.Gold,
                progress.MagicalSeed,
                progress.NextLifeCountdown));

            if (reloadCurrentLevel)
            {
                _eventPublisher.Publish(new GameDebugLevelReloadRequested(nextLevelNumber));
            }
        }

        public void SetGold(int amount)
        {
            var progress = _progressService.Current;
            _progressService.SetProgress(new MainMenuPlayerProgress(
                progress.LevelNumber,
                progress.CurrentLives,
                progress.MaxLives,
                Math.Max(0, amount),
                progress.MagicalSeed,
                progress.NextLifeCountdown));
        }

        public void AddGold(int amount)
        {
            SetGold(_progressService.Current.Gold + amount);
        }

        public void SetMagicalSeed(int amount)
        {
            var progress = _progressService.Current;
            _progressService.SetProgress(new MainMenuPlayerProgress(
                progress.LevelNumber,
                progress.CurrentLives,
                progress.MaxLives,
                progress.Gold,
                Math.Max(0, amount),
                progress.NextLifeCountdown));
        }

        public void AddMagicalSeed(int amount)
        {
            SetMagicalSeed(_progressService.Current.MagicalSeed + amount);
        }

        public void SetLives(int currentLives, int maxLives)
        {
            var progress = _progressService.Current;
            var normalizedMaxLives = Math.Max(1, maxLives);
            var normalizedCurrentLives = Math.Min(normalizedMaxLives, Math.Max(0, currentLives));
            _progressService.SetProgress(new MainMenuPlayerProgress(
                progress.LevelNumber,
                normalizedCurrentLives,
                normalizedMaxLives,
                progress.Gold,
                progress.MagicalSeed,
                progress.NextLifeCountdown));
        }

        public void RefillLives()
        {
            var progress = _progressService.Current;
            SetLives(progress.MaxLives, progress.MaxLives);
        }

        public void ResetProgress()
        {
            var defaultProgress = MainMenuPlayerProgress.Default;
            _levelSelectionWriter.SelectLevel(defaultProgress.LevelNumber);
            _progressService.SetProgress(defaultProgress);
            _eventPublisher.Publish(new GameDebugLevelReloadRequested(defaultProgress.LevelNumber));
        }

        private int NormalizeLevel(int levelNumber)
        {
            return Math.Max(1, levelNumber);
        }
    }
}
#endif
