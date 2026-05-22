namespace BlossomFlow.MainMenu.Domain
{
    public readonly struct MainMenuGardenHarvestResult
    {
        public MainMenuGardenHarvestResult(bool success, int regionIndex, int goldAwarded)
        {
            Success = success;
            RegionIndex = regionIndex;
            GoldAwarded = goldAwarded;
        }

        public bool Success { get; }
        public int RegionIndex { get; }
        public int GoldAwarded { get; }
    }
}
