namespace BlossomFlow.Gameplay.Presentation
{
    public readonly struct ConveyorPotCountChanged
    {
        public ConveyorPotCountChanged(int currentPots, int maxPots)
        {
            CurrentPots = currentPots;
            MaxPots = maxPots;
        }

        public int CurrentPots { get; }
        public int MaxPots { get; }
    }
}
