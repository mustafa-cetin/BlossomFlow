namespace BlossomFlow.MainMenu.Domain
{
    public interface IMainMenuPlayerProgressService
    {
        MainMenuPlayerProgress Current { get; }
        void AddGold(int amount);
        void AddGoldAndAdvanceLevel(int goldAmount);
        void SetProgress(MainMenuPlayerProgress progress);
    }
}
