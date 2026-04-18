using System;
using BlossomFlow.Gameplay.Domain.Levels;

namespace BlossomFlow.Application.Levels
{
    public sealed class LevelCatalog : ILevelCatalog
    {
        private readonly LevelCatalogAsset _asset;

        public LevelCatalog(LevelCatalogAsset asset)
        {
            _asset = asset;
        }

        public int LevelCount => _asset != null ? _asset.LevelCount : 0;

        public bool TryGetDefinition(int levelNumber, out LevelDefinition definition)
        {
            definition = null;

            if (_asset == null)
            {
                return false;
            }

            if (!_asset.TryGetDefinition(levelNumber, out definition))
            {
                return false;
            }

            try
            {
                definition.Validate();
                return true;
            }
            catch (Exception)
            {
                definition = null;
                throw;
            }
        }
    }
}
