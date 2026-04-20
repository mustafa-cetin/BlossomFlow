namespace BlossomFlow.Application.Levels
{
    public readonly struct RemoteLevelCatalogUnavailable
    {
        public RemoteLevelCatalogUnavailable(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }
}
