namespace BlossomFlow.MainMenu.Domain
{
    public readonly struct MainMenuTabChanged
    {
        public MainMenuTabChanged(MainMenuTab previousTab, MainMenuTab currentTab)
        {
            PreviousTab = previousTab;
            CurrentTab = currentTab;
        }

        public MainMenuTab PreviousTab { get; }
        public MainMenuTab CurrentTab { get; }
        public int Direction => (int)CurrentTab - (int)PreviousTab;
    }

    public readonly struct MainMenuPlayRequested
    {
        public MainMenuPlayRequested(int levelNumber)
        {
            LevelNumber = levelNumber;
        }

        public int LevelNumber { get; }
    }

    public readonly struct MainMenuPlayerProgressChanged
    {
        public MainMenuPlayerProgressChanged(MainMenuPlayerProgress progress)
        {
            Progress = progress;
        }

        public MainMenuPlayerProgress Progress { get; }
    }

    public readonly struct MainMenuGardenChanged
    {
        public MainMenuGardenChanged(MainMenuGardenState state)
        {
            State = state;
        }

        public MainMenuGardenState State { get; }
    }
}
