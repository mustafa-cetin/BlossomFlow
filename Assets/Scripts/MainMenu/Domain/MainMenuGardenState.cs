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
            return TryGetPlot(regionIndex, out var plot) && plot.ReadyUtcTicks > 0L;
        }

        public bool IsGrowing(int regionIndex, long nowUtcTicks)
        {
            return TryGetPlot(regionIndex, out var plot) && plot.ReadyUtcTicks > 0L && !plot.IsGrown(nowUtcTicks);
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
                if (_plots[i].ReadyUtcTicks > 0L && !_plots[i].IsGrown(nowUtcTicks))
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

        public MainMenuGardenState WithPlanted(int regionIndex, long readyUtcTicks, int plotLevel = 0)
        {
            if (regionIndex < 0 || IsPlanted(regionIndex))
            {
                return this;
            }

            var effectivePlotLevel = plotLevel > 0 ? plotLevel : 1;
            var existingIndex = FindPlotIndex(regionIndex);
            if (existingIndex >= 0)
            {
                var nextPlots = new MainMenuGardenPlotState[_plots.Length];
                Array.Copy(_plots, nextPlots, _plots.Length);
                nextPlots[existingIndex] = new MainMenuGardenPlotState(regionIndex, readyUtcTicks, effectivePlotLevel, 0);
                return new MainMenuGardenState(nextPlots);
            }

            var currentLength = _plots?.Length ?? 0;
            var newPlots = new MainMenuGardenPlotState[currentLength + 1];
            if (currentLength > 0)
            {
                Array.Copy(_plots, newPlots, currentLength);
            }

            newPlots[currentLength] = new MainMenuGardenPlotState(regionIndex, readyUtcTicks, effectivePlotLevel, 0);
            return new MainMenuGardenState(newPlots);
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
                if (plot.ReadyUtcTicks <= 0L || plot.IsGrown(nowUtcTicks))
                {
                    nextPlots[i] = plot;
                    continue;
                }

                var readyUtcTicks = Math.Max(nowUtcTicks, plot.ReadyUtcTicks - reductionTicks);
                nextPlots[i] = new MainMenuGardenPlotState(plot.RegionIndex, readyUtcTicks, plot.PlotLevel, plot.PlantId);
            }

            return new MainMenuGardenState(nextPlots);
        }

        public MainMenuGardenState WithHarvested(int regionIndex)
        {
            if (_plots == null)
            {
                return this;
            }

            var nextPlots = new MainMenuGardenPlotState[_plots.Length];
            for (var i = 0; i < _plots.Length; i++)
            {
                if (_plots[i].RegionIndex == regionIndex)
                {
                    var oldPlot = _plots[i];
                    nextPlots[i] = new MainMenuGardenPlotState(
                        oldPlot.RegionIndex,
                        0L,
                        oldPlot.PlotLevel + 1,
                        oldPlot.PlantId);
                }
                else
                {
                    nextPlots[i] = _plots[i];
                }
            }

            return new MainMenuGardenState(nextPlots);
        }

        public int GetGrownPlotCount(long nowUtcTicks)
        {
            if (_plots == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < _plots.Length; i++)
            {
                if (_plots[i].IsGrown(nowUtcTicks))
                {
                    count++;
                }
            }

            return count;
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

        private int FindPlotIndex(int regionIndex)
        {
            if (_plots == null)
            {
                return -1;
            }

            for (var i = 0; i < _plots.Length; i++)
            {
                if (_plots[i].RegionIndex == regionIndex)
                {
                    return i;
                }
            }

            return -1;
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
