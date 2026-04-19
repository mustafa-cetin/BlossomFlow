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
        bool TryPlant(int regionIndex, MainMenuGardenTiming timing);
        bool TryWater(MainMenuGardenTiming timing);
        void SetState(MainMenuGardenState state);
    }
}
