namespace BlossomFlow.MainMenu.Domain
{
    public interface IMainMenuPlayerProgressService
    {
        MainMenuPlayerProgress Current { get; }
        void AddGold(int amount);
        void AddGoldAndAdvanceLevel(int goldAmount);
        void AddMagicalSeed(int amount);
        bool TrySpendGold(int amount);
        bool TrySpendMagicalSeed(int amount);
        bool TryConsumeLife();
        void SetProgress(MainMenuPlayerProgress progress);
    }
}
