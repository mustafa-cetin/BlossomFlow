namespace BlossomFlow.Gameplay.Presentation
{
    public readonly struct ConveyorPotEntryBlocked
    {
        public ConveyorPotEntryBlocked(int currentPots, int maxPots)
        {
            CurrentPots = currentPots;
            MaxPots = maxPots;
        }

        public int CurrentPots { get; }
        public int MaxPots { get; }
    }
}
