using System;
using System.Collections.Generic;
using BlossomFlow.Gameplay.Domain.Levels;

namespace BlossomFlow.Application.Levels
{
    public sealed class LevelCatalog : ILevelCatalog, IDisposable
    {
        private readonly LevelCatalogAsset _asset;
        private readonly Dictionary<int, LevelDefinition> _loadedDefinitions = new();
        private bool _isDisposed;

        public LevelCatalog(LevelCatalogAsset asset)
        {
            _asset = asset;
        }

        public int LevelCount => _asset != null ? _asset.LevelCount : 0;

        public bool TryGetDefinition(int levelNumber, out LevelDefinition definition)
        {
            definition = null;

            if (_loadedDefinitions.TryGetValue(levelNumber, out definition) && definition != null)
            {
                return true;
            }

            if (_asset == null || !_asset.TryGetDefinition(levelNumber, out definition))
            {
                return false;
            }

            ValidateDefinition(definition);
            _loadedDefinitions[levelNumber] = definition;
            return true;
        }

        public IDisposable LoadDefinition(int levelNumber, Action<LevelDefinition> onLoaded, Action<Exception> onFailed)
        {
            if (_asset == null)
            {
                onFailed?.Invoke(new InvalidOperationException("Level catalog asset is missing."));
                return DisposableAction.Empty;
            }

            if (TryGetDefinition(levelNumber, out var cachedDefinition))
            {
                onLoaded?.Invoke(cachedDefinition);
                return DisposableAction.Empty;
            }

            onFailed?.Invoke(new InvalidOperationException($"Level {levelNumber} is missing from the local level catalog."));
            return DisposableAction.Empty;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _loadedDefinitions.Clear();
        }

        private static void ValidateDefinition(LevelDefinition definition)
        {
            if (definition == null)
            {
                throw new InvalidOperationException("Level definition is missing.");
            }

            definition.Validate();
        }

        private sealed class DisposableAction : IDisposable
        {
            public static readonly DisposableAction Empty = new DisposableAction();

            public void Dispose()
            {
            }
        }
    }
}
