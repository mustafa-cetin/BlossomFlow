namespace BlossomFlow.MainMenu.Domain
{
    public readonly struct MainMenuPlayerProgress
    {
        public MainMenuPlayerProgress(int levelNumber, int currentLives, int maxLives, int gold, string nextLifeCountdown)
        {
            LevelNumber = levelNumber;
            CurrentLives = currentLives;
            MaxLives = maxLives;
            Gold = gold;
            NextLifeCountdown = nextLifeCountdown;
        }

        public int LevelNumber { get; }
        public int CurrentLives { get; }
        public int MaxLives { get; }
        public int Gold { get; }
        public string NextLifeCountdown { get; }
        public bool HasLifeCountdown => !string.IsNullOrWhiteSpace(NextLifeCountdown);

        public static MainMenuPlayerProgress Default => new(1, 5, 5, 0, string.Empty);
    }
}
