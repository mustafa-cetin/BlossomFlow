using System;
using System.Globalization;
using BlossomFlow.Application.Events;

namespace BlossomFlow.MainMenu.Domain
{
    public sealed class MainMenuGardenService : IGardenService
    {
        private const int GardenUnlockLevel = 50;
        private const string PlantedRegionIndexesKey = "blossom_flow.garden.planted_region_indexes";
        private const string NextWaterAvailableUtcTicksKey = "blossom_flow.garden.next_water_available_utc_ticks";

        private readonly IMainMenuPlayerProgressService _progressService;
        private readonly IEventPublisher _eventPublisher;
        private MainMenuGardenState _current = MainMenuGardenState.Empty;
        private long _nextWaterAvailableUtcTicks;
        private bool _isLoaded;

        public MainMenuGardenService(
            IMainMenuPlayerProgressService progressService,
            IEventPublisher eventPublisher)
        {
            _progressService = progressService;
            _eventPublisher = eventPublisher;
        }

        public MainMenuGardenState Current
        {
            get
            {
                EnsureLoaded();
                return _current;
            }
        }

        public int UnlockLevel => GardenUnlockLevel;

        public bool IsUnlocked => _progressService.Current.LevelNumber >= UnlockLevel;

        public bool CanPlant => IsUnlocked && _progressService.Current.MagicalSeed > 0;

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

            if (_current.IsPlanted(regionIndex))
            {
                return false;
            }

            if (!_progressService.TrySpendMagicalSeed(1))
            {
                return false;
            }

            var readyUtcTicks = DateTime.UtcNow.Ticks + timing.GrowthDuration.Ticks;
            SetState(_current.WithPlanted(regionIndex, readyUtcTicks));
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
            SaveState(_current, _nextWaterAvailableUtcTicks);
            _eventPublisher.Publish(new MainMenuGardenChanged(_current));
            return true;
        }

        public void SetState(MainMenuGardenState state)
        {
            _current = state;
            _isLoaded = true;
            SaveState(_current, _nextWaterAvailableUtcTicks);
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
            _isLoaded = true;
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

            plot = new MainMenuGardenPlotState(regionIndex, readyUtcTicks);
            return true;
        }

        private static long ReadLong(string key, long defaultValue)
        {
            var value = SafePlayerPrefs.GetString(key, string.Empty);
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue)
                ? parsedValue
                : defaultValue;
        }

        private static void SaveState(MainMenuGardenState state, long nextWaterAvailableUtcTicks)
        {
            var plots = state.GetPlots();
            if (plots.Length == 0)
            {
                SafePlayerPrefs.SetString(PlantedRegionIndexesKey, string.Empty);
                SafePlayerPrefs.SetString(NextWaterAvailableUtcTicksKey, nextWaterAvailableUtcTicks > 0L ? nextWaterAvailableUtcTicks.ToString(CultureInfo.InvariantCulture) : string.Empty);
                SafePlayerPrefs.Save();
                return;
            }

            var serializedPlots = new string[plots.Length];
            for (var i = 0; i < plots.Length; i++)
            {
                serializedPlots[i] = string.Concat(
                    plots[i].RegionIndex.ToString(CultureInfo.InvariantCulture),
                    ":",
                    plots[i].ReadyUtcTicks.ToString(CultureInfo.InvariantCulture));
            }

            var serialized = string.Join(";", serializedPlots);
            SafePlayerPrefs.SetString(PlantedRegionIndexesKey, serialized);
            SafePlayerPrefs.SetString(NextWaterAvailableUtcTicksKey, nextWaterAvailableUtcTicks > 0L ? nextWaterAvailableUtcTicks.ToString(CultureInfo.InvariantCulture) : string.Empty);
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
