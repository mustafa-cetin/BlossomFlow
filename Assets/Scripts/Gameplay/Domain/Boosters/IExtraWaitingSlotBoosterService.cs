namespace BlossomFlow.Gameplay.Domain.Boosters
{
    public interface IExtraWaitingSlotBoosterService
    {
        int RemainingUses { get; }
        bool IsUnlocked { get; }
        void ResetForLevel();
        void SetUnlockedForLevel(int currentLevelNumber);
        bool TryUse(LevelSession session, out int slotIndex);
    }
}
