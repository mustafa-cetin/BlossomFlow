using System.Collections;
using BlossomFlow.Application.Levels;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class StartupServiceInitializer : IStartupServiceInitializer
    {
        private readonly ByteBrewStartupServiceInitializer _byteBrewInitializer;
        private readonly CrazyGamesStartupServiceInitializer _crazyGamesInitializer;
        private readonly IRemoteLevelCatalogPreloader _levelCatalogPreloader;

        public StartupServiceInitializer(
            ByteBrewStartupServiceInitializer byteBrewInitializer,
            CrazyGamesStartupServiceInitializer crazyGamesInitializer,
            IRemoteLevelCatalogPreloader levelCatalogPreloader)
        {
            _byteBrewInitializer = byteBrewInitializer;
            _crazyGamesInitializer = crazyGamesInitializer;
            _levelCatalogPreloader = levelCatalogPreloader;
        }

        public IEnumerator InitializeIfNeeded()
        {
            if (_byteBrewInitializer != null)
            {
                yield return _byteBrewInitializer.InitializeIfNeeded();
            }

            if (_crazyGamesInitializer != null)
            {
                yield return _crazyGamesInitializer.InitializeIfNeeded();
            }

            if (_levelCatalogPreloader != null)
            {
                yield return _levelCatalogPreloader.LoadIfNeeded();
            }
        }
    }
}
