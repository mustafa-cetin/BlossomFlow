namespace BlossomFlow.Gameplay.Application.Levels
{
    public readonly struct LevelAdvanceRequested
    {
        public LevelAdvanceRequested(int levelNumber)
        {
            LevelNumber = levelNumber;
        }

        public int LevelNumber { get; }
    }

    public readonly struct LevelAdvanced
    {
        public LevelAdvanced(int levelNumber)
        {
            LevelNumber = levelNumber;
        }

        public int LevelNumber { get; }
    }
}
