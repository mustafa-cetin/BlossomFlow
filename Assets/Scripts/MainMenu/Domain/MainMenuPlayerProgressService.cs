using System;
using BlossomFlow.Application.Events;

namespace BlossomFlow.MainMenu.Domain
{
    public sealed class MainMenuPlayerProgressService : IMainMenuPlayerProgressService
    {
        private const int LifeRecoveryMinutes = 10;
        private const long LifeRecoveryTicks = TimeSpan.TicksPerMinute * LifeRecoveryMinutes;
        private const string LevelNumberKey = "blossom_flow.progress.level_number";
        private const string CurrentLivesKey = "blossom_flow.progress.current_lives";
        private const string MaxLivesKey = "blossom_flow.progress.max_lives";
        private const string GoldKey = "blossom_flow.progress.gold";
        private const string NextLifeCountdownKey = "blossom_flow.progress.next_life_countdown";
        private const string NextLifeReadyUtcTicksKey = "blossom_flow.progress.next_life_ready_utc_ticks";

        private readonly IEventPublisher _eventPublisher;
        private MainMenuPlayerProgress _current = MainMenuPlayerProgress.Default;
        private long _nextLifeReadyUtcTicks;
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
                RefreshLifeRecovery(true);
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
            RefreshLifeRecovery(true);
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
            RefreshLifeRecovery(true);
            SetProgress(new MainMenuPlayerProgress(
                _current.LevelNumber + 1,
                _current.CurrentLives,
                _current.MaxLives,
                _current.Gold + Math.Max(0, goldAmount),
                _current.NextLifeCountdown));
        }

        public bool TryConsumeLife()
        {
            EnsureLoaded();
            RefreshLifeRecovery(true);

            if (_current.CurrentLives <= 0)
            {
                return false;
            }

            var nextLives = Math.Max(0, _current.CurrentLives - 1);
            if (nextLives < _current.MaxLives && _nextLifeReadyUtcTicks <= 0)
            {
                _nextLifeReadyUtcTicks = DateTime.UtcNow.Ticks + LifeRecoveryTicks;
            }

            SetProgress(new MainMenuPlayerProgress(
                _current.LevelNumber,
                nextLives,
                _current.MaxLives,
                _current.Gold,
                GetCurrentLifeCountdown()));
            return true;
        }

        public void SetProgress(MainMenuPlayerProgress progress)
        {
            _current = NormalizeProgress(progress);
            _isLoaded = true;
            UpdateLifeTimerForCurrentProgress();
            UpdateCurrentLifeCountdown();
            SaveProgress(_current, _nextLifeReadyUtcTicks);
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
            _nextLifeReadyUtcTicks = ReadLong(NextLifeReadyUtcTicksKey, 0L);
            _isLoaded = true;
        }

        private void RefreshLifeRecovery(bool publishIfChanged)
        {
            if (_current.CurrentLives >= _current.MaxLives)
            {
                if (_nextLifeReadyUtcTicks != 0L || _current.HasLifeCountdown)
                {
                    _nextLifeReadyUtcTicks = 0L;
                    _current = WithLives(_current, _current.MaxLives, string.Empty);
                    SaveProgress(_current, _nextLifeReadyUtcTicks);
                }

                return;
            }

            if (_nextLifeReadyUtcTicks <= 0L)
            {
                _nextLifeReadyUtcTicks = DateTime.UtcNow.Ticks + LifeRecoveryTicks;
                UpdateCurrentLifeCountdown();
                SaveProgress(_current, _nextLifeReadyUtcTicks);
                return;
            }

            var nowTicks = DateTime.UtcNow.Ticks;
            if (nowTicks < _nextLifeReadyUtcTicks)
            {
                UpdateCurrentLifeCountdown();
                return;
            }

            var missingLives = _current.MaxLives - _current.CurrentLives;
            var recoveredLives = Math.Min(
                missingLives,
                (int)((nowTicks - _nextLifeReadyUtcTicks) / LifeRecoveryTicks) + 1);
            var nextLives = _current.CurrentLives + recoveredLives;

            if (nextLives >= _current.MaxLives)
            {
                _nextLifeReadyUtcTicks = 0L;
                _current = WithLives(_current, _current.MaxLives, string.Empty);
            }
            else
            {
                _nextLifeReadyUtcTicks += LifeRecoveryTicks * recoveredLives;
                _current = WithLives(_current, nextLives, GetCurrentLifeCountdown());
            }

            SaveProgress(_current, _nextLifeReadyUtcTicks);

            if (publishIfChanged)
            {
                _eventPublisher.Publish(new MainMenuPlayerProgressChanged(_current));
            }
        }

        private void UpdateLifeTimerForCurrentProgress()
        {
            if (_current.CurrentLives >= _current.MaxLives)
            {
                _nextLifeReadyUtcTicks = 0L;
                return;
            }

            if (_nextLifeReadyUtcTicks <= 0L)
            {
                _nextLifeReadyUtcTicks = DateTime.UtcNow.Ticks + LifeRecoveryTicks;
            }
        }

        private void UpdateCurrentLifeCountdown()
        {
            _current = WithLives(_current, _current.CurrentLives, GetCurrentLifeCountdown());
        }

        private string GetCurrentLifeCountdown()
        {
            if (_current.CurrentLives >= _current.MaxLives || _nextLifeReadyUtcTicks <= 0L)
            {
                return string.Empty;
            }

            var remainingTicks = Math.Max(0L, _nextLifeReadyUtcTicks - DateTime.UtcNow.Ticks);
            var remaining = TimeSpan.FromTicks(remainingTicks);
            return remaining.TotalHours >= 1d
                ? $"{(int)remaining.TotalHours}:{remaining.Minutes:00}:{remaining.Seconds:00}"
                : $"{remaining.Minutes:00}:{remaining.Seconds:00}";
        }

        private static MainMenuPlayerProgress NormalizeProgress(MainMenuPlayerProgress progress)
        {
            var maxLives = Math.Max(1, progress.MaxLives);
            return new MainMenuPlayerProgress(
                Math.Max(1, progress.LevelNumber),
                Math.Min(maxLives, Math.Max(0, progress.CurrentLives)),
                maxLives,
                Math.Max(0, progress.Gold),
                progress.NextLifeCountdown ?? string.Empty);
        }

        private static MainMenuPlayerProgress WithLives(MainMenuPlayerProgress progress, int lives, string countdown)
        {
            return new MainMenuPlayerProgress(
                progress.LevelNumber,
                lives,
                progress.MaxLives,
                progress.Gold,
                countdown ?? string.Empty);
        }

        private static long ReadLong(string key, long defaultValue)
        {
            var value = SafePlayerPrefs.GetString(key, string.Empty);
            return long.TryParse(value, out var parsedValue) ? parsedValue : defaultValue;
        }

        private static void SaveProgress(MainMenuPlayerProgress progress, long nextLifeReadyUtcTicks)
        {
            SafePlayerPrefs.SetInt(LevelNumberKey, progress.LevelNumber);
            SafePlayerPrefs.SetInt(CurrentLivesKey, progress.CurrentLives);
            SafePlayerPrefs.SetInt(MaxLivesKey, progress.MaxLives);
            SafePlayerPrefs.SetInt(GoldKey, progress.Gold);
            SafePlayerPrefs.SetString(NextLifeCountdownKey, progress.NextLifeCountdown ?? string.Empty);
            SafePlayerPrefs.SetString(NextLifeReadyUtcTicksKey, nextLifeReadyUtcTicks > 0L ? nextLifeReadyUtcTicks.ToString() : string.Empty);
            SafePlayerPrefs.Save();
        }
    }
}
