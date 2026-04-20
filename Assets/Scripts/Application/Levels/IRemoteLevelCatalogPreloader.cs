using System.Collections;

namespace BlossomFlow.Application.Levels
{
    public interface IRemoteLevelCatalogPreloader
    {
        bool IsLoaded { get; }
        bool IsUnavailable { get; }
        string UnavailableReason { get; }
        IEnumerator LoadIfNeeded();
        void RetryLoad();
    }
}
