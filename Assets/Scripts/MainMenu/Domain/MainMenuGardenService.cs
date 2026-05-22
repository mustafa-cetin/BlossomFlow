using System;
using System.Globalization;
using BlossomFlow.Application.Events;

namespace BlossomFlow.MainMenu.Domain
{
    public sealed class MainMenuGardenService : IGardenService
    {
        private const string PlantedRegionIndexesKey = "blossom_flow.garden.planted_region_indexes";
        private const string NextWaterAvailableUtcTicksKey = "blossom_flow.garden.next_water_available_utc_ticks";
        private const string UnlockedPlotCountKey = "blossom_flow.garden.unlocked_plot_count";

        private readonly IMainMenuPlayerProgressService _progressService;
        private readonly IEventPublisher _eventPublisher;
        private readonly MainMenuGardenConfig _config;
        private MainMenuGardenState _current = MainMenuGardenState.Empty;
        private long _nextWaterAvailableUtcTicks;
        private int _unlockedPlotCount;
        private bool _isLoaded;

        public MainMenuGardenService(
            IMainMenuPlayerProgressService progressService,
            IEventPublisher eventPublisher,
            MainMenuGardenConfig config)
        {
            _progressService = progressService;
            _eventPublisher = eventPublisher;
            _config = config;
        }

        public MainMenuGardenState Current
        {
            get
            {
                EnsureLoaded();
                return _current;
            }
        }

        public int UnlockLevel => _config.UnlockLevel;

        public bool IsUnlocked => _progressService.Current.LevelNumber >= UnlockLevel;

        public bool CanPlant
        {
            get
            {
                EnsureLoaded();
                return IsUnlocked && _progressService.Current.MagicalSeed > 0 && _unlockedPlotCount > 0;
            }
        }

        public bool CanWater
        {
            get
            {
                EnsureLoaded();
                var nowTicks = DateTime.UtcNow.Ticks;
                return IsUnlocked
                    && _current.HasGrowingPlots(nowTicks)
                    && nowTicks >= _nextWaterAvailableUtcTicks;
            }
        }

        public string WaterCountdown
        {
            get
            {
                EnsureLoaded();
                var nowTicks = DateTime.UtcNow.Ticks;
                if (!_current.HasGrowingPlots(nowTicks))
                {
                    return string.Empty;
                }

                if (nowTicks >= _nextWaterAvailableUtcTicks)
                {
                    return string.Empty;
                }

                return FormatCountdown(TimeSpan.FromTicks(_nextWaterAvailableUtcTicks - nowTicks));
            }
        }

        public int UnlockedPlotCount
        {
            get
            {
                EnsureLoaded();
                return _unlockedPlotCount;
            }
        }

        public bool CanHarvest(int regionIndex)
        {
            EnsureLoaded();
            return IsUnlocked && _current.IsGrown(regionIndex, DateTime.UtcNow.Ticks);
        }

        public bool CanUnlockPlot
        {
            get
            {
                EnsureLoaded();
                var cost = GetNextUnlockCost();
                return IsUnlocked && _progressService.Current.Gold >= cost;
            }
        }

        public bool TryPlant(int regionIndex, MainMenuGardenTiming timing)
        {
            if (regionIndex < 0)
            {
                return false;
            }

            EnsureLoaded();
            if (!IsUnlocked)
            {
                return false;
            }

            if (regionIndex >= _unlockedPlotCount)
            {
                return false;
            }

            if (_current.IsPlanted(regionIndex))
            {
                return false;
            }

            if (!_progressService.TrySpendMagicalSeed(1))
            {
                return false;
            }

            var readyUtcTicks = DateTime.UtcNow.Ticks + timing.GrowthDuration.Ticks;
            var currentPlotLevel = GetPlotLevel(regionIndex);
            var nextPlotLevel = Math.Max(1, currentPlotLevel);
            SetState(_current.WithPlanted(regionIndex, readyUtcTicks, nextPlotLevel));
            return true;
        }

        public bool TryWater(MainMenuGardenTiming timing)
        {
            EnsureLoaded();
            var nowTicks = DateTime.UtcNow.Ticks;
            if (!IsUnlocked || !_current.HasGrowingPlots(nowTicks) || nowTicks < _nextWaterAvailableUtcTicks)
            {
                return false;
            }

            _current = _current.WithGrowthReduced(nowTicks, timing.WaterReduction.Ticks);
            _nextWaterAvailableUtcTicks = nowTicks + timing.WaterCooldown.Ticks;
            SaveState(_current, _nextWaterAvailableUtcTicks, _unlockedPlotCount);
            _eventPublisher.Publish(new MainMenuGardenChanged(_current));
            return true;
        }

        public MainMenuGardenHarvestResult TryHarvest(int regionIndex)
        {
            EnsureLoaded();
            var nowTicks = DateTime.UtcNow.Ticks;
            if (!IsUnlocked || !_current.IsGrown(regionIndex, nowTicks))
            {
                return new MainMenuGardenHarvestResult(false, regionIndex, 0);
            }

            var goldReward = _config.HarvestGoldReward;
            _progressService.AddGold(goldReward);
            _current = _current.WithHarvested(regionIndex);
            SaveState(_current, _nextWaterAvailableUtcTicks, _unlockedPlotCount);
            _eventPublisher.Publish(new MainMenuGardenChanged(_current));
            return new MainMenuGardenHarvestResult(true, regionIndex, goldReward);
        }

        public bool TryUnlockPlot()
        {
            EnsureLoaded();
            var cost = GetNextUnlockCost();
            if (!IsUnlocked || _progressService.Current.Gold < cost)
            {
                return false;
            }

            if (!_progressService.TrySpendGold(cost))
            {
                return false;
            }

            _unlockedPlotCount++;
            SaveState(_current, _nextWaterAvailableUtcTicks, _unlockedPlotCount);
            _eventPublisher.Publish(new MainMenuGardenChanged(_current));
            return true;
        }

        public void SetState(MainMenuGardenState state)
        {
            _current = state;
            _isLoaded = true;
            SaveState(_current, _nextWaterAvailableUtcTicks, _unlockedPlotCount);
            _eventPublisher.Publish(new MainMenuGardenChanged(_current));
        }

        public void SetState(MainMenuGardenState state, long nextWaterAvailableUtcTicks, int unlockedPlotCount)
        {
            _current = state;
            _nextWaterAvailableUtcTicks = nextWaterAvailableUtcTicks;
            _unlockedPlotCount = unlockedPlotCount;
            _isLoaded = true;
            SaveState(_current, _nextWaterAvailableUtcTicks, _unlockedPlotCount);
            _eventPublisher.Publish(new MainMenuGardenChanged(_current));
        }

        private void EnsureLoaded()
        {
            if (_isLoaded)
            {
                return;
            }

            _current = new MainMenuGardenState(ReadPlantedRegionIndexes());
            _nextWaterAvailableUtcTicks = ReadLong(NextWaterAvailableUtcTicksKey, 0L);
            _unlockedPlotCount = Math.Max(_config.FreePlotCount, SafePlayerPrefs.GetInt(UnlockedPlotCountKey, _config.FreePlotCount));
            _isLoaded = true;
        }

        private int GetPlotLevel(int regionIndex)
        {
            var plots = _current.GetPlots();
            for (var i = 0; i < plots.Length; i++)
            {
                if (plots[i].RegionIndex == regionIndex)
                {
                    return plots[i].PlotLevel;
                }
            }

            return 0;
        }

        private int GetNextUnlockCost()
        {
            var purchasedCount = Math.Max(0, _unlockedPlotCount - _config.FreePlotCount);
            return _config.PlotUnlockGoldCost + purchasedCount * _config.PlotUnlockGoldCostIncrement;
        }

        private static MainMenuGardenPlotState[] ReadPlantedRegionIndexes()
        {
            var serialized = SafePlayerPrefs.GetString(PlantedRegionIndexesKey, string.Empty);
            if (string.IsNullOrWhiteSpace(serialized))
            {
                return Array.Empty<MainMenuGardenPlotState>();
            }

            var separator = serialized.Contains(";") ? ';' : ',';
            var values = serialized.Split(separator);
            var plots = new MainMenuGardenPlotState[values.Length];
            var count = 0;
            for (var i = 0; i < values.Length; i++)
            {
                if (!TryParsePlot(values[i], out var plot))
                {
                    continue;
                }

                plots[count] = plot;
                count++;
            }

            if (count == plots.Length)
            {
                return plots;
            }

            var trimmed = new MainMenuGardenPlotState[count];
            Array.Copy(plots, trimmed, count);
            return trimmed;
        }

        private static bool TryParsePlot(string value, out MainMenuGardenPlotState plot)
        {
            plot = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var parts = value.Split(':');
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var regionIndex))
            {
                return false;
            }

            var readyUtcTicks = 0L;
            if (parts.Length > 1)
            {
                long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out readyUtcTicks);
            }

            var plotLevel = 0;
            if (parts.Length > 2)
            {
                int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out plotLevel);
            }

            var plantId = 0;
            if (parts.Length > 3)
            {
                int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out plantId);
            }

            if (plotLevel <= 0 && readyUtcTicks > 0L)
            {
                plotLevel = 1;
            }

            plot = new MainMenuGardenPlotState(regionIndex, readyUtcTicks, plotLevel, plantId);
            return true;
        }

        private static long ReadLong(string key, long defaultValue)
        {
            var value = SafePlayerPrefs.GetString(key, string.Empty);
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue)
                ? parsedValue
                : defaultValue;
        }

        private static void SaveState(MainMenuGardenState state, long nextWaterAvailableUtcTicks, int unlockedPlotCount)
        {
            var plots = state.GetPlots();
            if (plots.Length == 0)
            {
                SafePlayerPrefs.SetString(PlantedRegionIndexesKey, string.Empty);
                SafePlayerPrefs.SetString(NextWaterAvailableUtcTicksKey, nextWaterAvailableUtcTicks > 0L ? nextWaterAvailableUtcTicks.ToString(CultureInfo.InvariantCulture) : string.Empty);
                SafePlayerPrefs.SetInt(UnlockedPlotCountKey, unlockedPlotCount);
                SafePlayerPrefs.Save();
                return;
            }

            var serializedPlots = new string[plots.Length];
            for (var i = 0; i < plots.Length; i++)
            {
                serializedPlots[i] = string.Concat(
                    plots[i].RegionIndex.ToString(CultureInfo.InvariantCulture),
                    ":",
                    plots[i].ReadyUtcTicks.ToString(CultureInfo.InvariantCulture),
                    ":",
                    plots[i].PlotLevel.ToString(CultureInfo.InvariantCulture),
                    ":",
                    plots[i].PlantId.ToString(CultureInfo.InvariantCulture));
            }

            var serialized = string.Join(";", serializedPlots);
            SafePlayerPrefs.SetString(PlantedRegionIndexesKey, serialized);
            SafePlayerPrefs.SetString(NextWaterAvailableUtcTicksKey, nextWaterAvailableUtcTicks > 0L ? nextWaterAvailableUtcTicks.ToString(CultureInfo.InvariantCulture) : string.Empty);
            SafePlayerPrefs.SetInt(UnlockedPlotCountKey, unlockedPlotCount);
            SafePlayerPrefs.Save();
        }

        private static string FormatCountdown(TimeSpan remaining)
        {
            return remaining.TotalHours >= 1d
                ? $"{(int)remaining.TotalHours}:{remaining.Minutes:00}:{remaining.Seconds:00}"
                : $"{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
    }
}
