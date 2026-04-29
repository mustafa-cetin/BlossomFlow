namespace BlossomFlow.Application.Platform.CrazyGames
{
    public sealed class CompositeGameplaySessionReporter : IGameplayPlatformSessionReporter
    {
        private readonly IGameplayPlatformSessionReporter[] _reporters;

        public CompositeGameplaySessionReporter(params IGameplayPlatformSessionReporter[] reporters)
        {
            _reporters = reporters ?? new IGameplayPlatformSessionReporter[0];
        }

        public void NotifyGameplayStart()
        {
            for (var i = 0; i < _reporters.Length; i++)
            {
                _reporters[i]?.NotifyGameplayStart();
            }
        }

        public void NotifyGameplayStop()
        {
            for (var i = 0; i < _reporters.Length; i++)
            {
                _reporters[i]?.NotifyGameplayStop();
            }
        }
    }
}
