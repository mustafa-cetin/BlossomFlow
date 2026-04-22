namespace BlossomFlow.Application.Platform.CrazyGames
{
    public static class CrazyGamesSdkRuntime
    {
        public static bool IsEnabled { get; private set; }

        public static void Configure(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }
    }
}
