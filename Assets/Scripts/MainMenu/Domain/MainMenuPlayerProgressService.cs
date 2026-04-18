using System;
using BlossomFlow.Application.Events;

namespace BlossomFlow.MainMenu.Domain
{
    public sealed class MainMenuPlayerProgressService : IMainMenuPlayerProgressService
    {
        private const string LevelNumberKey = "blossom_flow.progress.level_number";
        private const string CurrentLivesKey = "blossom_flow.progress.current_lives";
        private const string MaxLivesKey = "blossom_flow.progress.max_lives";
        private const string GoldKey = "blossom_flow.progress.gold";
        private const string NextLifeCountdownKey = "blossom_flow.progress.next_life_countdown";

        private readonly IEventPublisher _eventPublisher;
        private MainMenuPlayerProgress _current = MainMenuPlayerProgress.Default;
        private bool _isLoaded;

        public MainMenuPlayerProgressService(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        public MainMenuPlayerProgress Current
        {
            get
            {
                EnsureLoaded();
                return _current;
            }
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            EnsureLoaded();
            SetProgress(new MainMenuPlayerProgress(
                _current.LevelNumber,
                _current.CurrentLives,
                _current.MaxLives,
                _current.Gold + amount,
                _current.NextLifeCountdown));
        }

        public void AddGoldAndAdvanceLevel(int goldAmount)
        {
            EnsureLoaded();
            SetProgress(new MainMenuPlayerProgress(
                _current.LevelNumber + 1,
                _current.CurrentLives,
                _current.MaxLives,
                _current.Gold + Math.Max(0, goldAmount),
                _current.NextLifeCountdown));
        }

        public void SetProgress(MainMenuPlayerProgress progress)
        {
            _current = progress;
            _isLoaded = true;
            SaveProgress(_current);
            _eventPublisher.Publish(new MainMenuPlayerProgressChanged(_current));
        }

        private void EnsureLoaded()
        {
            if (_isLoaded)
            {
                return;
            }

            var fallback = MainMenuPlayerProgress.Default;
            var maxLives = Math.Max(1, SafePlayerPrefs.GetInt(MaxLivesKey, fallback.MaxLives));
            _current = new MainMenuPlayerProgress(
                Math.Max(1, SafePlayerPrefs.GetInt(LevelNumberKey, fallback.LevelNumber)),
                Math.Min(maxLives, Math.Max(0, SafePlayerPrefs.GetInt(CurrentLivesKey, fallback.CurrentLives))),
                maxLives,
                Math.Max(0, SafePlayerPrefs.GetInt(GoldKey, fallback.Gold)),
                SafePlayerPrefs.GetString(NextLifeCountdownKey, fallback.NextLifeCountdown));
            _isLoaded = true;
        }

        private static void SaveProgress(MainMenuPlayerProgress progress)
        {
            SafePlayerPrefs.SetInt(LevelNumberKey, progress.LevelNumber);
            SafePlayerPrefs.SetInt(CurrentLivesKey, progress.CurrentLives);
            SafePlayerPrefs.SetInt(MaxLivesKey, progress.MaxLives);
            SafePlayerPrefs.SetInt(GoldKey, progress.Gold);
            SafePlayerPrefs.SetString(NextLifeCountdownKey, progress.NextLifeCountdown ?? string.Empty);
            SafePlayerPrefs.Save();
        }
    }
}
