using System.Collections;
using BlossomFlow.Application.Levels;
using BlossomFlow.Application.Performance;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class StartupServiceInitializer : IStartupServiceInitializer
    {
        private readonly CrazyGamesStartupServiceInitializer _crazyGamesInitializer;
        private readonly IRemoteLevelCatalogPreloader _levelCatalogPreloader;
        private readonly IFrameRateConfigurator _frameRateConfigurator;

        public StartupServiceInitializer(
            CrazyGamesStartupServiceInitializer crazyGamesInitializer,
            IRemoteLevelCatalogPreloader levelCatalogPreloader,
            IFrameRateConfigurator frameRateConfigurator)
        {
            _crazyGamesInitializer = crazyGamesInitializer;
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

            if (_levelCatalogPreloader != null)
            {
                yield return _levelCatalogPreloader.LoadIfNeeded();
            }
        }
    }
}
