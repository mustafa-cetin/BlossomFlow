namespace BlossomFlow.Gameplay.Domain.Boosters
{
    public interface IShuffleBoosterService
    {
        int RemainingUses { get; }
        void ResetForLevel();
        bool TryUse(LevelSession session);
    }
}
