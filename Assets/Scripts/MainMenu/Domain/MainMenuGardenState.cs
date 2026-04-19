using System;

namespace BlossomFlow.MainMenu.Domain
{
    public readonly struct MainMenuGardenState
    {
        private readonly MainMenuGardenPlotState[] _plots;

        public MainMenuGardenState(MainMenuGardenPlotState[] plots)
        {
            _plots = CopyAndSortUnique(plots);
        }

        public int PlantedCount => _plots?.Length ?? 0;

        public bool IsPlanted(int regionIndex)
        {
            return TryGetPlot(regionIndex, out _);
        }

        public bool IsGrowing(int regionIndex, long nowUtcTicks)
        {
            return TryGetPlot(regionIndex, out var plot) && !plot.IsGrown(nowUtcTicks);
        }

        public bool IsGrown(int regionIndex, long nowUtcTicks)
        {
            return TryGetPlot(regionIndex, out var plot) && plot.IsGrown(nowUtcTicks);
        }

        public TimeSpan GetRemainingGrowthTime(int regionIndex, long nowUtcTicks)
        {
            if (!TryGetPlot(regionIndex, out var plot))
            {
                return TimeSpan.Zero;
            }

            var remainingTicks = Math.Max(0L, plot.ReadyUtcTicks - nowUtcTicks);
            return TimeSpan.FromTicks(remainingTicks);
        }

        public bool HasGrowingPlots(long nowUtcTicks)
        {
            if (_plots == null)
            {
                return false;
            }

            for (var i = 0; i < _plots.Length; i++)
            {
                if (!_plots[i].IsGrown(nowUtcTicks))
                {
                    return true;
                }
            }

            return false;
        }

        public MainMenuGardenPlotState[] GetPlots()
        {
            if (_plots == null || _plots.Length == 0)
            {
                return Array.Empty<MainMenuGardenPlotState>();
            }

            var copy = new MainMenuGardenPlotState[_plots.Length];
            Array.Copy(_plots, copy, _plots.Length);
            return copy;
        }

        public MainMenuGardenState WithPlanted(int regionIndex, long readyUtcTicks)
        {
            if (regionIndex < 0 || IsPlanted(regionIndex))
            {
                return this;
            }

            var currentLength = _plots?.Length ?? 0;
            var nextPlots = new MainMenuGardenPlotState[currentLength + 1];
            if (currentLength > 0)
            {
                Array.Copy(_plots, nextPlots, currentLength);
            }

            nextPlots[currentLength] = new MainMenuGardenPlotState(regionIndex, readyUtcTicks);
            return new MainMenuGardenState(nextPlots);
        }

        public MainMenuGardenState WithGrowthReduced(long nowUtcTicks, long reductionTicks)
        {
            if (_plots == null || _plots.Length == 0 || reductionTicks <= 0L)
            {
                return this;
            }

            var nextPlots = new MainMenuGardenPlotState[_plots.Length];
            for (var i = 0; i < _plots.Length; i++)
            {
                var plot = _plots[i];
                if (plot.IsGrown(nowUtcTicks))
                {
                    nextPlots[i] = plot;
                    continue;
                }

                var readyUtcTicks = Math.Max(nowUtcTicks, plot.ReadyUtcTicks - reductionTicks);
                nextPlots[i] = new MainMenuGardenPlotState(plot.RegionIndex, readyUtcTicks);
            }

            return new MainMenuGardenState(nextPlots);
        }

        public static MainMenuGardenState Empty => new(Array.Empty<MainMenuGardenPlotState>());

        private bool TryGetPlot(int regionIndex, out MainMenuGardenPlotState plot)
        {
            plot = default;
            if (_plots == null)
            {
                return false;
            }

            for (var i = 0; i < _plots.Length; i++)
            {
                if (_plots[i].RegionIndex != regionIndex)
                {
                    continue;
                }

                plot = _plots[i];
                return true;
            }

            return false;
        }

        private static MainMenuGardenPlotState[] CopyAndSortUnique(MainMenuGardenPlotState[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<MainMenuGardenPlotState>();
            }

            var copy = new MainMenuGardenPlotState[source.Length];
            var count = 0;
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                if (value.RegionIndex < 0)
                {
                    continue;
                }

                copy[count] = value;
                count++;
            }

            if (count == 0)
            {
                return Array.Empty<MainMenuGardenPlotState>();
            }

            Array.Sort(copy, 0, count, MainMenuGardenPlotStateComparer.Instance);

            var uniqueCount = 1;
            for (var i = 1; i < count; i++)
            {
                if (copy[i].RegionIndex == copy[uniqueCount - 1].RegionIndex)
                {
                    continue;
                }

                copy[uniqueCount] = copy[i];
                uniqueCount++;
            }

            if (uniqueCount == copy.Length)
            {
                return copy;
            }

            var unique = new MainMenuGardenPlotState[uniqueCount];
            Array.Copy(copy, unique, uniqueCount);
            return unique;
        }

        private sealed class MainMenuGardenPlotStateComparer : System.Collections.Generic.IComparer<MainMenuGardenPlotState>
        {
            public static readonly MainMenuGardenPlotStateComparer Instance = new();

            public int Compare(MainMenuGardenPlotState x, MainMenuGardenPlotState y)
            {
                return x.RegionIndex.CompareTo(y.RegionIndex);
            }
        }
    }
}
