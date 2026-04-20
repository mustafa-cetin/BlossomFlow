#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace BlossomFlow.Application.Debugging
{
    public readonly struct GameDebugSnapshot
    {
        public GameDebugSnapshot(
            int levelNumber,
            int selectedLevelNumber,
            int levelCount,
            int currentLives,
            int maxLives,
            int gold,
            int magicalSeed,
            string nextLifeCountdown)
        {
            LevelNumber = levelNumber;
            SelectedLevelNumber = selectedLevelNumber;
            LevelCount = levelCount;
            CurrentLives = currentLives;
            MaxLives = maxLives;
            Gold = gold;
            MagicalSeed = magicalSeed;
            NextLifeCountdown = nextLifeCountdown;
        }

        public int LevelNumber { get; }
        public int SelectedLevelNumber { get; }
        public int LevelCount { get; }
        public int CurrentLives { get; }
        public int MaxLives { get; }
        public int Gold { get; }
        public int MagicalSeed { get; }
        public string NextLifeCountdown { get; }
    }
}
#endif
