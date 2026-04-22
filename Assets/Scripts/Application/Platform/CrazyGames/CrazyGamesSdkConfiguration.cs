namespace BlossomFlow.Application.Platform.CrazyGames
{
    public sealed class CrazyGamesSdkConfiguration
    {
        public CrazyGamesSdkConfiguration(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        public bool IsEnabled { get; }
    }
}
