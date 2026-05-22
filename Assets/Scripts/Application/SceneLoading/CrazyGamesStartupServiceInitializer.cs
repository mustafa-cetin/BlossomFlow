using System.Collections;
using BlossomFlow.Application.Platform.CrazyGames;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class CrazyGamesStartupServiceInitializer : IStartupServiceInitializer
    {
        private bool _hasInitialized;

        public CrazyGamesStartupServiceInitializer(CrazyGamesSdkConfiguration configuration)
        {
        }

        public IEnumerator InitializeIfNeeded()
        {
            if (_hasInitialized)
            {
                yield break;
            }

            CrazyGamesSdkRuntime.Configure(false);
            _hasInitialized = true;
        }
    }
}
