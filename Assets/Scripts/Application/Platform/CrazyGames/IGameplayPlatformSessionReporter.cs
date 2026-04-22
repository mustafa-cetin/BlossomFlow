namespace BlossomFlow.Application.Platform.CrazyGames
{
    public interface IGameplayPlatformSessionReporter
    {
        void NotifyGameplayStart();

        void NotifyGameplayStop();
    }
}
