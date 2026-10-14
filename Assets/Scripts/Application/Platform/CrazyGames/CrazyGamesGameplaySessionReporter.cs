namespace BlossomFlow.Application.Platform.CrazyGames
{
    public sealed class CrazyGamesGameplaySessionReporter : IGameplayPlatformSessionReporter
    {
        private readonly CrazyGamesSdkConfiguration _configuration;
        private bool _isGameplayReportedActive;

        public CrazyGamesGameplaySessionReporter(CrazyGamesSdkConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void NotifyGameplayStart()
        {
            if (_isGameplayReportedActive || _configuration == null || !_configuration.IsEnabled)
            {
                return;
            }

            _isGameplayReportedActive = true;
        }

        public void NotifyGameplayStop()
        {
            if (!_isGameplayReportedActive)
            {
                return;
            }

            _isGameplayReportedActive = false;
        }
    }
}
