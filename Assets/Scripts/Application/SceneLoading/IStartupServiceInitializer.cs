using System.Collections;

namespace BlossomFlow.Application.SceneLoading
{
    public interface IStartupServiceInitializer
    {
        IEnumerator InitializeIfNeeded();
    }
}
