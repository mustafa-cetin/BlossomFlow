namespace BlossomFlow.Gameplay.Domain.Boosters
{
    public readonly struct ExtraWaitingSlotBoosterRequested
    {
    }

    public readonly struct ExtraWaitingSlotBoosterUsed
    {
        public ExtraWaitingSlotBoosterUsed(int remainingUses, int slotIndex)
        {
            RemainingUses = remainingUses;
            SlotIndex = slotIndex;
        }

        public int RemainingUses { get; }
        public int SlotIndex { get; }
    }

    public readonly struct ExtraWaitingSlotBoosterUnavailable
    {
        public ExtraWaitingSlotBoosterUnavailable(int remainingUses)
        {
            RemainingUses = remainingUses;
        }

        public int RemainingUses { get; }
    }

    public readonly struct ExtraWaitingSlotBoosterUsesChanged
    {
        public ExtraWaitingSlotBoosterUsesChanged(int remainingUses)
        {
            RemainingUses = remainingUses;
        }

        public int RemainingUses { get; }
    }
}
