namespace BlossomFlow.MainMenu.Domain
{
    public interface IMainMenuNavigator
    {
        MainMenuTab CurrentTab { get; }
        bool TrySelect(MainMenuTab tab);
        bool TryMove(int direction);
    }
}
