#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace BlossomFlow.Application.Debugging
{
    public readonly struct GameDebugLevelReloadRequested
    {
        public GameDebugLevelReloadRequested(int levelNumber)
        {
            LevelNumber = levelNumber;
        }

        public int LevelNumber { get; }
    }
}
#endif
