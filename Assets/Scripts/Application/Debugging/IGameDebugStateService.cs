#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace BlossomFlow.Application.Debugging
{
    public interface IGameDebugStateService
    {
        GameDebugSnapshot Current { get; }
        void SetLevel(int levelNumber, bool reloadCurrentLevel);
        void SetGold(int amount);
        void AddGold(int amount);
        void SetMagicalSeed(int amount);
        void AddMagicalSeed(int amount);
        void SetLives(int currentLives, int maxLives);
        void RefillLives();
        void ResetProgress();
    }
}
#endif
