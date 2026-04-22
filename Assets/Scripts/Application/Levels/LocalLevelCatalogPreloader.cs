using System.Collections;

namespace BlossomFlow.Application.Levels
{
    public sealed class LocalLevelCatalogPreloader : IRemoteLevelCatalogPreloader
    {
        public bool IsLoaded => true;

        public bool IsUnavailable => false;

        public string UnavailableReason => null;

        public IEnumerator LoadIfNeeded()
        {
            yield break;
        }

        public void RetryLoad()
        {
        }
    }
}
