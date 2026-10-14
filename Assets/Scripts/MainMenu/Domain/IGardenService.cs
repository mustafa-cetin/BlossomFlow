namespace BlossomFlow.MainMenu.Domain
{
    public interface IGardenService
    {
        MainMenuGardenState Current { get; }
        int UnlockLevel { get; }
        bool IsUnlocked { get; }
        bool CanPlant { get; }
        bool CanWater { get; }
        string WaterCountdown { get; }
        int UnlockedPlotCount { get; }
        bool CanHarvest(int regionIndex);
        bool CanUnlockPlot { get; }
        bool TryPlant(int regionIndex, MainMenuGardenTiming timing);
        bool TryWater(MainMenuGardenTiming timing);
        MainMenuGardenHarvestResult TryHarvest(int regionIndex);
        bool TryUnlockPlot();
        void SetState(MainMenuGardenState state);
    }
}
