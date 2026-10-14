using System;

namespace BlossomFlow.Gameplay.Domain.Boosters
{
    public sealed class ExtraWaitingSlotBoosterService : IExtraWaitingSlotBoosterService
    {
        public const int DefaultUsesPerLevel = 1;
        public const int UnlockLevel = 50;

        public ExtraWaitingSlotBoosterService()
        {
            ResetForLevel();
        }

        public int RemainingUses { get; private set; }
        public bool IsUnlocked { get; private set; }

        public void ResetForLevel()
        {
            RemainingUses = DefaultUsesPerLevel;
            IsUnlocked = false;
        }

        public void SetUnlockedForLevel(int currentLevelNumber)
        {
            IsUnlocked = currentLevelNumber >= UnlockLevel;
            if (!IsUnlocked)
            {
                RemainingUses = 0;
            }
        }

        public bool TryUse(LevelSession session, out int slotIndex)
        {
            slotIndex = -1;

            if (!IsUnlocked ||
                RemainingUses <= 0 ||
                session == null ||
                session.State != LevelPlayState.Playing)
            {
                return false;
            }

            if (!session.TryAddExtraWaitingSlot(out slotIndex))
            {
                return false;
            }

            RemainingUses--;
            return true;
        }
    }
}
