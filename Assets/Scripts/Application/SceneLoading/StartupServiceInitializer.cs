using System.Collections;
using BlossomFlow.Application.Levels;
using BlossomFlow.Application.Performance;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class StartupServiceInitializer : IStartupServiceInitializer
    {
        private readonly CrazyGamesStartupServiceInitializer _crazyGamesInitializer;
        private readonly ByteBrewStartupServiceInitializer _byteBrewInitializer;
        private readonly IRemoteLevelCatalogPreloader _levelCatalogPreloader;
        private readonly IFrameRateConfigurator _frameRateConfigurator;

        public StartupServiceInitializer(
            CrazyGamesStartupServiceInitializer crazyGamesInitializer,
            ByteBrewStartupServiceInitializer byteBrewInitializer,
            IRemoteLevelCatalogPreloader levelCatalogPreloader,
            IFrameRateConfigurator frameRateConfigurator)
        {
            _crazyGamesInitializer = crazyGamesInitializer;
            _byteBrewInitializer = byteBrewInitializer;
            _levelCatalogPreloader = levelCatalogPreloader;
            _frameRateConfigurator = frameRateConfigurator;
        }

        public IEnumerator InitializeIfNeeded()
        {
            _frameRateConfigurator?.Apply();

            if (_crazyGamesInitializer != null)
            {
                yield return _crazyGamesInitializer.InitializeIfNeeded();
            }

            if (_byteBrewInitializer != null)
            {
                yield return _byteBrewInitializer.InitializeIfNeeded();
            }

            if (_levelCatalogPreloader != null)
            {
                yield return _levelCatalogPreloader.LoadIfNeeded();
            }
        }
    }
}
