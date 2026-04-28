namespace BlossomFlow.Gameplay.Presentation
{
    public readonly struct LevelVictoryScreenRequested
    {
    }

    public readonly struct LevelFailureScreenRequested
    {
        public LevelFailureScreenRequested(bool skipKeepPlayingOffer)
        {
            SkipKeepPlayingOffer = skipKeepPlayingOffer;
        }

        public bool SkipKeepPlayingOffer { get; }
    }

    public readonly struct LevelRestarted
    {
    }

    public readonly struct LevelRestartRequested
    {
    }

    public readonly struct LevelKeepPlayingRequested
    {
    }

    public readonly struct LevelKeepPlayingStarted
    {
    }
}
