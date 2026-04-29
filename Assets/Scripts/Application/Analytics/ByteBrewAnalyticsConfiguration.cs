namespace BlossomFlow.Application.Analytics
{
    public sealed class ByteBrewAnalyticsConfiguration
    {
        public ByteBrewAnalyticsConfiguration(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        public bool IsEnabled { get; }
    }
}
