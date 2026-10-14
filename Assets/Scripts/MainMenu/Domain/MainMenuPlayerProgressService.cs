using System;
using BlossomFlow.Application.Events;
using BlossomFlow.Application.Levels;

namespace BlossomFlow.MainMenu.Domain
{
    public sealed class MainMenuPlayerProgressService : IMainMenuPlayerProgressService
    {
        private const int LifeRecoveryMinutes = 10;
        private const int GardenUnlockLevel = 20;
        private const int GardenUnlockMagicalSeedReward = 1;
        private const long LifeRecoveryTicks = TimeSpan.TicksPerMinute * LifeRecoveryMinutes;
        private const string LevelNumberKey = "blossom_flow.progress.level_number";
        private const string CurrentLivesKey = "blossom_flow.progress.current_lives";
        private const string MaxLivesKey = "blossom_flow.progress.max_lives";
        private const string GoldKey = "blossom_flow.progress.gold";
        private const string MagicalSeedKey = "blossom_flow.progress.magical_seed";
        private const string GardenUnlockSeedGrantedKey = "blossom_flow.progress.garden_unlock_seed_granted";
        private const string PendingCampaignLevelNumberKey = "blossom_flow.progress.pending_campaign_level_number";
        private const string NextLifeCountdownKey = "blossom_flow.progress.next_life_countdown";
        private const string NextLifeReadyUtcTicksKey = "blossom_flow.progress.next_life_ready_utc_ticks";

        private readonly IEventPublisher _eventPublisher;
        private readonly ILevelCatalog _levelCatalog;
        private readonly int _legacyOvershootReturnLevelNumber;
        private MainMenuPlayerProgress _current = MainMenuPlayerProgress.Default;
        private long _nextLifeReadyUtcTicks;
        private int _pendingCampaignLevelNumber;
        private bool _gardenUnlockSeedGranted;
        private bool _isLoaded;

        public MainMenuPlayerProgressService(
            IEventPublisher eventPublisher,
            ILevelCatalog levelCatalog,
            int legacyOvershootReturnLevelNumber)
        {
            _eventPublisher = eventPublisher;
            _levelCatalog = levelCatalog;
            _legacyOvershootReturnLevelNumber = Math.Max(1, legacyOvershootReturnLevelNumber);
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
                _current.MagicalSeed,
                _current.NextLifeCountdown));
        }

        public void AdvanceLevel()
        {
            EnsureLoaded();
            RefreshLifeRecovery(true);
            var nextLevelNumber = _current.LevelNumber + 1;
            SetProgress(new MainMenuPlayerProgress(
                nextLevelNumber,
                _current.CurrentLives,
                _current.MaxLives,
                _current.Gold,
                GetMagicalSeedAfterLevelAdvance(nextLevelNumber),
                _current.NextLifeCountdown));
        }

        public void AddGoldAndAdvanceLevel(int goldAmount)
        {
            EnsureLoaded();
            RefreshLifeRecovery(true);
            var nextLevelNumber = _current.LevelNumber + 1;
            SetProgress(new MainMenuPlayerProgress(
                nextLevelNumber,
                _current.CurrentLives,
                _current.MaxLives,
                _current.Gold + Math.Max(0, goldAmount),
                GetMagicalSeedAfterLevelAdvance(nextLevelNumber),
                _current.NextLifeCountdown));
        }

        public void AddMagicalSeed(int amount)
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
                _current.Gold,
                _current.MagicalSeed + amount,
                _current.NextLifeCountdown));
        }

        public void AddLife(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            EnsureLoaded();
            RefreshLifeRecovery(true);

            if (_current.CurrentLives >= _current.MaxLives)
            {
                return;
            }

            SetProgress(new MainMenuPlayerProgress(
                _current.LevelNumber,
                Math.Min(_current.MaxLives, _current.CurrentLives + amount),
                _current.MaxLives,
                _current.Gold,
                _current.MagicalSeed,
                _current.NextLifeCountdown));
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            EnsureLoaded();
            RefreshLifeRecovery(true);

            if (_current.Gold < amount)
            {
                return false;
            }

            SetProgress(new MainMenuPlayerProgress(
                _current.LevelNumber,
                _current.CurrentLives,
                _current.MaxLives,
                _current.Gold - amount,
                _current.MagicalSeed,
                _current.NextLifeCountdown));
            return true;
        }

        public bool TrySpendMagicalSeed(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            EnsureLoaded();
            RefreshLifeRecovery(true);

            if (_current.MagicalSeed < amount)
            {
                return false;
            }

            SetProgress(new MainMenuPlayerProgress(
                _current.LevelNumber,
                _current.CurrentLives,
                _current.MaxLives,
                _current.Gold,
                _current.MagicalSeed - amount,
                _current.NextLifeCountdown));
            return true;
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
                _current.MagicalSeed,
                GetCurrentLifeCountdown()));
            return true;
        }

        public void SetProgress(MainMenuPlayerProgress progress)
        {
            _current = NormalizeProgress(progress);
            _isLoaded = true;
            UpdatePendingCampaignLevelForCurrentProgress();
            if (!_gardenUnlockSeedGranted &&
                _current.LevelNumber >= GardenUnlockLevel &&
                _current.MagicalSeed <= 0)
            {
                _current = new MainMenuPlayerProgress(
                    _current.LevelNumber,
                    _current.CurrentLives,
                    _current.MaxLives,
                    _current.Gold,
                    GardenUnlockMagicalSeedReward,
                    _current.NextLifeCountdown);
                _gardenUnlockSeedGranted = true;
            }
            else if (_current.LevelNumber >= GardenUnlockLevel || _current.MagicalSeed > 0)
            {
                _gardenUnlockSeedGranted = true;
            }

            UpdateLifeTimerForCurrentProgress();
            UpdateCurrentLifeCountdown();
            SaveProgress(_current, _nextLifeReadyUtcTicks, _pendingCampaignLevelNumber);
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
            var loadedProgress = new MainMenuPlayerProgress(
                Math.Max(1, SafePlayerPrefs.GetInt(LevelNumberKey, fallback.LevelNumber)),
                Math.Min(maxLives, Math.Max(0, SafePlayerPrefs.GetInt(CurrentLivesKey, fallback.CurrentLives))),
                maxLives,
                Math.Max(0, SafePlayerPrefs.GetInt(GoldKey, fallback.Gold)),
                Math.Max(0, SafePlayerPrefs.GetInt(MagicalSeedKey, fallback.MagicalSeed)),
                SafePlayerPrefs.GetString(NextLifeCountdownKey, fallback.NextLifeCountdown));
            var loadedPendingCampaignLevelNumber = Math.Max(0, SafePlayerPrefs.GetInt(PendingCampaignLevelNumberKey, 0));
            _pendingCampaignLevelNumber = loadedPendingCampaignLevelNumber;
            _current = NormalizeLoadedProgress(loadedProgress);
            _nextLifeReadyUtcTicks = ReadLong(NextLifeReadyUtcTicksKey, 0L);
            _gardenUnlockSeedGranted = SafePlayerPrefs.GetInt(GardenUnlockSeedGrantedKey, 0) == 1;
            _isLoaded = true;
            if (_current.LevelNumber != loadedProgress.LevelNumber ||
                _pendingCampaignLevelNumber != loadedPendingCampaignLevelNumber)
            {
                SaveProgress(_current, _nextLifeReadyUtcTicks, _pendingCampaignLevelNumber);
            }

            ApplyGardenUnlockSeedMigration();
        }

        private void RefreshLifeRecovery(bool publishIfChanged)
        {
            if (_current.CurrentLives >= _current.MaxLives)
            {
                if (_nextLifeReadyUtcTicks != 0L || _current.HasLifeCountdown)
                {
                    _nextLifeReadyUtcTicks = 0L;
                    _current = WithLives(_current, _current.MaxLives, string.Empty);
                    SaveProgress(_current, _nextLifeReadyUtcTicks, _pendingCampaignLevelNumber);
                }

                return;
            }

            if (_nextLifeReadyUtcTicks <= 0L)
            {
                _nextLifeReadyUtcTicks = DateTime.UtcNow.Ticks + LifeRecoveryTicks;
                UpdateCurrentLifeCountdown();
                SaveProgress(_current, _nextLifeReadyUtcTicks, _pendingCampaignLevelNumber);
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

            SaveProgress(_current, _nextLifeReadyUtcTicks, _pendingCampaignLevelNumber);

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

        private MainMenuPlayerProgress NormalizeProgress(MainMenuPlayerProgress progress)
        {
            var maxLives = Math.Max(1, progress.MaxLives);
            return new MainMenuPlayerProgress(
                ClampLevelNumber(progress.LevelNumber),
                Math.Min(maxLives, Math.Max(0, progress.CurrentLives)),
                maxLives,
                Math.Max(0, progress.Gold),
                Math.Max(0, progress.MagicalSeed),
                progress.NextLifeCountdown ?? string.Empty);
        }

        private MainMenuPlayerProgress NormalizeLoadedProgress(MainMenuPlayerProgress progress)
        {
            var levelCount = GetCatalogLevelCount();
            if (_pendingCampaignLevelNumber > 0 && levelCount >= _pendingCampaignLevelNumber)
            {
                var pendingLevelNumber = _pendingCampaignLevelNumber;
                _pendingCampaignLevelNumber = 0;
                return NormalizeProgress(new MainMenuPlayerProgress(
                    pendingLevelNumber,
                    progress.CurrentLives,
                    progress.MaxLives,
                    progress.Gold,
                    progress.MagicalSeed,
                    progress.NextLifeCountdown));
            }

            if (_pendingCampaignLevelNumber <= 0 && levelCount > 0 && progress.LevelNumber > levelCount)
            {
                _pendingCampaignLevelNumber = _legacyOvershootReturnLevelNumber > levelCount
                    ? _legacyOvershootReturnLevelNumber
                    : 0;
                return NormalizeProgress(new MainMenuPlayerProgress(
                    _legacyOvershootReturnLevelNumber,
                    progress.CurrentLives,
                    progress.MaxLives,
                    progress.Gold,
                    progress.MagicalSeed,
                    progress.NextLifeCountdown));
            }

            return NormalizeProgress(progress);
        }

        private int ClampLevelNumber(int levelNumber)
        {
            var minLevelNumber = 1;
            return Math.Max(minLevelNumber, levelNumber);
        }

        private void UpdatePendingCampaignLevelForCurrentProgress()
        {
            var levelCount = GetCatalogLevelCount();
            if (levelCount <= 0)
            {
                return;
            }

            if (_pendingCampaignLevelNumber > 0 && levelCount >= _pendingCampaignLevelNumber)
            {
                _current = WithLevel(_current, _pendingCampaignLevelNumber);
                _pendingCampaignLevelNumber = 0;
                return;
            }

            if (_current.LevelNumber > levelCount && _pendingCampaignLevelNumber <= 0)
            {
                _pendingCampaignLevelNumber = levelCount + 1;
                return;
            }

            if (_current.LevelNumber <= levelCount)
            {
                _pendingCampaignLevelNumber = 0;
            }
        }

        private int GetCatalogLevelCount()
        {
            return _levelCatalog != null ? _levelCatalog.LevelCount : 0;
        }

        private static MainMenuPlayerProgress WithLives(MainMenuPlayerProgress progress, int lives, string countdown)
        {
            return new MainMenuPlayerProgress(
                progress.LevelNumber,
                lives,
                progress.MaxLives,
                progress.Gold,
                progress.MagicalSeed,
                countdown ?? string.Empty);
        }

        private static MainMenuPlayerProgress WithLevel(MainMenuPlayerProgress progress, int levelNumber)
        {
            return new MainMenuPlayerProgress(
                Math.Max(1, levelNumber),
                progress.CurrentLives,
                progress.MaxLives,
                progress.Gold,
                progress.MagicalSeed,
                progress.NextLifeCountdown ?? string.Empty);
        }

        private int GetMagicalSeedAfterLevelAdvance(int nextLevelNumber)
        {
            if (_current.LevelNumber < GardenUnlockLevel &&
                nextLevelNumber >= GardenUnlockLevel &&
                !_gardenUnlockSeedGranted &&
                _current.MagicalSeed <= 0)
            {
                _gardenUnlockSeedGranted = true;
                return GardenUnlockMagicalSeedReward;
            }

            if (nextLevelNumber >= GardenUnlockLevel && !_gardenUnlockSeedGranted && _current.MagicalSeed > 0)
            {
                _gardenUnlockSeedGranted = true;
            }

            return _current.MagicalSeed;
        }

        private void ApplyGardenUnlockSeedMigration()
        {
            if (_gardenUnlockSeedGranted || _current.LevelNumber < GardenUnlockLevel)
            {
                return;
            }

            _gardenUnlockSeedGranted = true;
            if (_current.MagicalSeed <= 0)
            {
                _current = new MainMenuPlayerProgress(
                    _current.LevelNumber,
                    _current.CurrentLives,
                    _current.MaxLives,
                    _current.Gold,
                    GardenUnlockMagicalSeedReward,
                    _current.NextLifeCountdown);
            }

            SaveProgress(_current, _nextLifeReadyUtcTicks, _pendingCampaignLevelNumber);
            _eventPublisher.Publish(new MainMenuPlayerProgressChanged(_current));
        }

        private static long ReadLong(string key, long defaultValue)
        {
            var value = SafePlayerPrefs.GetString(key, string.Empty);
            return long.TryParse(value, out var parsedValue) ? parsedValue : defaultValue;
        }

        private static void SaveProgress(
            MainMenuPlayerProgress progress,
            long nextLifeReadyUtcTicks,
            int pendingCampaignLevelNumber)
        {
            SafePlayerPrefs.SetInt(LevelNumberKey, progress.LevelNumber);
            SafePlayerPrefs.SetInt(CurrentLivesKey, progress.CurrentLives);
            SafePlayerPrefs.SetInt(MaxLivesKey, progress.MaxLives);
            SafePlayerPrefs.SetInt(GoldKey, progress.Gold);
            SafePlayerPrefs.SetInt(MagicalSeedKey, progress.MagicalSeed);
            SafePlayerPrefs.SetInt(GardenUnlockSeedGrantedKey, progress.LevelNumber >= GardenUnlockLevel || progress.MagicalSeed > 0 ? 1 : 0);
            SafePlayerPrefs.SetInt(PendingCampaignLevelNumberKey, Math.Max(0, pendingCampaignLevelNumber));
            SafePlayerPrefs.SetString(NextLifeCountdownKey, progress.NextLifeCountdown ?? string.Empty);
            SafePlayerPrefs.SetString(NextLifeReadyUtcTicksKey, nextLifeReadyUtcTicks > 0L ? nextLifeReadyUtcTicks.ToString() : string.Empty);
            SafePlayerPrefs.Save();
        }
    }
}
