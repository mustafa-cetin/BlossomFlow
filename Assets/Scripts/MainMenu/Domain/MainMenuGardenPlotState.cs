namespace BlossomFlow.MainMenu.Domain
{
    public readonly struct MainMenuGardenPlotState
    {
        public MainMenuGardenPlotState(int regionIndex, long readyUtcTicks)
            : this(regionIndex, readyUtcTicks, 0, 0)
        {
        }

        public MainMenuGardenPlotState(int regionIndex, long readyUtcTicks, int plotLevel, int plantId)
        {
            RegionIndex = regionIndex;
            ReadyUtcTicks = readyUtcTicks;
            PlotLevel = plotLevel;
            PlantId = plantId;
        }

        public int RegionIndex { get; }
        public long ReadyUtcTicks { get; }
        public int PlotLevel { get; }
        public int PlantId { get; }

        public bool IsGrown(long nowUtcTicks)
        {
            return ReadyUtcTicks > 0L && ReadyUtcTicks <= nowUtcTicks;
        }
    }
}
