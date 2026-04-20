using System;
using System.Collections.Generic;
using BlossomFlow.Gameplay.Domain.Levels;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BlossomFlow.Application.Levels
{
    public sealed class LevelCatalog : ILevelCatalog, IDisposable
    {
        private readonly LevelCatalogAsset _asset;
        private readonly Dictionary<int, LevelDefinition> _loadedDefinitions = new();
        private readonly Dictionary<int, AsyncOperationHandle<LevelDefinitionAsset>> _loadedHandles = new();
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
            if (TryGetDefinition(levelNumber, out var cachedDefinition))
            {
                onLoaded?.Invoke(cachedDefinition);
                return DisposableAction.Empty;
            }

            if (_asset == null)
            {
                onFailed?.Invoke(new InvalidOperationException("Level catalog asset is missing."));
                return DisposableAction.Empty;
            }

            if (!_asset.TryGetLevelReference(levelNumber, out var levelReference))
            {
                onFailed?.Invoke(new InvalidOperationException($"Level {levelNumber} does not have a valid Addressables reference."));
                return DisposableAction.Empty;
            }

            try
            {
                var handle = LoadHandle(levelNumber, levelReference);
                return new LevelLoadRequest(this, levelNumber, handle, onLoaded, onFailed);
            }
            catch (Exception exception)
            {
                onFailed?.Invoke(exception);
                return DisposableAction.Empty;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            foreach (var handle in _loadedHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            _loadedHandles.Clear();
            _loadedDefinitions.Clear();
        }

        private AsyncOperationHandle<LevelDefinitionAsset> LoadHandle(
            int levelNumber,
            AssetReferenceT<LevelDefinitionAsset> levelReference)
        {
            if (_loadedHandles.TryGetValue(levelNumber, out var existingHandle) && existingHandle.IsValid())
            {
                return existingHandle;
            }

            var handle = levelReference.LoadAssetAsync();
            _loadedHandles[levelNumber] = handle;
            return handle;
        }

        private bool TryCacheLoadedDefinition(
            int levelNumber,
            LevelDefinitionAsset levelAsset,
            out LevelDefinition definition,
            out Exception exception)
        {
            definition = null;
            exception = null;

            if (levelAsset == null)
            {
                exception = new InvalidOperationException($"Level {levelNumber} loaded without a level asset.");
                return false;
            }

            try
            {
                definition = levelAsset.Definition;
                definition.Validate();
                _loadedDefinitions[levelNumber] = definition;
                return true;
            }
            catch (Exception caughtException)
            {
                definition = null;
                exception = caughtException;
                return false;
            }
        }

        private static void ValidateDefinition(LevelDefinition definition)
        {
            if (definition == null)
            {
                throw new InvalidOperationException("Level definition is missing.");
            }

            definition.Validate();
        }

        private sealed class LevelLoadRequest : IDisposable
        {
            private readonly LevelCatalog _catalog;
            private readonly int _levelNumber;
            private AsyncOperationHandle<LevelDefinitionAsset> _handle;
            private Action<LevelDefinition> _onLoaded;
            private Action<Exception> _onFailed;
            private bool _isDisposed;

            public LevelLoadRequest(
                LevelCatalog catalog,
                int levelNumber,
                AsyncOperationHandle<LevelDefinitionAsset> handle,
                Action<LevelDefinition> onLoaded,
                Action<Exception> onFailed)
            {
                _catalog = catalog;
                _levelNumber = levelNumber;
                _handle = handle;
                _onLoaded = onLoaded;
                _onFailed = onFailed;
                _handle.Completed += OnCompleted;
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                _onLoaded = null;
                _onFailed = null;

                if (_handle.IsValid())
                {
                    _handle.Completed -= OnCompleted;
                }
            }

            private void OnCompleted(AsyncOperationHandle<LevelDefinitionAsset> handle)
            {
                if (_isDisposed)
                {
                    return;
                }

                if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
                {
                    var operationException = handle.OperationException;
                    _catalog.ReleaseLoadedHandle(_levelNumber);
                    _onFailed?.Invoke(operationException);
                    return;
                }

                if (_catalog.TryCacheLoadedDefinition(_levelNumber, handle.Result, out var definition, out var exception))
                {
                    _onLoaded?.Invoke(definition);
                    return;
                }

                _catalog.ReleaseLoadedHandle(_levelNumber);
                _onFailed?.Invoke(exception);
            }
        }

        private void ReleaseLoadedHandle(int levelNumber)
        {
            if (!_loadedHandles.TryGetValue(levelNumber, out var handle))
            {
                return;
            }

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            _loadedHandles.Remove(levelNumber);
            _loadedDefinitions.Remove(levelNumber);
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
