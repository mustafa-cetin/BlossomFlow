using System.Collections;

namespace BlossomFlow.Application.Levels
{
    public interface IRemoteLevelCatalogPreloader
    {
        IEnumerator LoadIfNeeded();
    }
}
