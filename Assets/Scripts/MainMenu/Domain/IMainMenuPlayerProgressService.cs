namespace BlossomFlow.MainMenu.Domain
{
    public interface IMainMenuPlayerProgressService
    {
        MainMenuPlayerProgress Current { get; }
        void SetProgress(MainMenuPlayerProgress progress);
    }
}
