namespace BlossomFlow.Gameplay.Domain.Boosters
{
    public readonly struct ShuffleBoosterRequested
    {
    }

    public readonly struct ShuffleBoosterUsed
    {
        public ShuffleBoosterUsed(int remainingUses)
        {
            RemainingUses = remainingUses;
        }

        public int RemainingUses { get; }
    }

    public readonly struct ShuffleBoosterUnavailable
    {
        public ShuffleBoosterUnavailable(int remainingUses)
        {
            RemainingUses = remainingUses;
        }

        public int RemainingUses { get; }
    }

    public readonly struct ShuffleBoosterUsesChanged
    {
        public ShuffleBoosterUsesChanged(int remainingUses)
        {
            RemainingUses = remainingUses;
        }

        public int RemainingUses { get; }
    }
}
