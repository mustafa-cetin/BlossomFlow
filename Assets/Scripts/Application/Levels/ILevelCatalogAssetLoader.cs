using System;

namespace BlossomFlow.Application.Levels
{
    public interface ILevelCatalogAssetLoader
    {
        IDisposable Load(Action<LevelCatalogAsset> onLoaded, Action<Exception> onFailed);
    }
}
