namespace BlossomFlow.MainMenu.Domain
{
    public readonly struct MainMenuGardenPlotState
    {
        public MainMenuGardenPlotState(int regionIndex, long readyUtcTicks)
        {
            RegionIndex = regionIndex;
            ReadyUtcTicks = readyUtcTicks;
        }

        public int RegionIndex { get; }
        public long ReadyUtcTicks { get; }

        public bool IsGrown(long nowUtcTicks)
        {
            return ReadyUtcTicks <= nowUtcTicks;
        }
    }
}
