using System;
using System.Collections;
using BlossomFlow.Gameplay.Domain.Levels;
using UnityEngine;

namespace BlossomFlow.Application.Levels
{
    public sealed class RemoteLevelCatalog : ILevelCatalog, IRemoteLevelCatalogPreloader, IDisposable
    {
        private readonly LevelCatalog _fallbackCatalog;
        private readonly ILevelCatalogAssetLoader _remoteCatalogLoader;
        private readonly object _syncRoot = new object();
        private IDisposable _activeLoad;
        private LevelCatalog _remoteCatalog;
        private bool _hasRemoteLoadFinished;
        private bool _hasRemoteLoadStarted;
        private bool _isDisposed;

        public RemoteLevelCatalog(LevelCatalogAsset fallbackCatalogAsset, ILevelCatalogAssetLoader remoteCatalogLoader)
        {
            _fallbackCatalog = new LevelCatalog(fallbackCatalogAsset);
            _remoteCatalogLoader = remoteCatalogLoader;
            EnsureRemoteLoadStarted();
        }

        public int LevelCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return ActiveCatalog.LevelCount;
                }
            }
        }

        private ILevelCatalog ActiveCatalog => _remoteCatalog ?? _fallbackCatalog;

        public bool TryGetDefinition(int levelNumber, out LevelDefinition definition)
        {
            EnsureRemoteLoadStarted();

            lock (_syncRoot)
            {
                return ActiveCatalog.TryGetDefinition(levelNumber, out definition);
            }
        }

        public IEnumerator LoadIfNeeded()
        {
            EnsureRemoteLoadStarted();

            while (!_hasRemoteLoadFinished)
            {
                yield return null;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _activeLoad?.Dispose();
            _activeLoad = null;
            _remoteCatalog = null;
            _hasRemoteLoadFinished = true;
        }

        private void EnsureRemoteLoadStarted()
        {
            if (_hasRemoteLoadStarted)
            {
                return;
            }

            _hasRemoteLoadStarted = true;

            if (_remoteCatalogLoader == null)
            {
                _hasRemoteLoadFinished = true;
                return;
            }

            _activeLoad = _remoteCatalogLoader.Load(UseRemoteCatalog, _ => _hasRemoteLoadFinished = true);
        }

        private void UseRemoteCatalog(LevelCatalogAsset catalogAsset)
        {
            if (_isDisposed || catalogAsset == null)
            {
                _hasRemoteLoadFinished = true;
                return;
            }

            lock (_syncRoot)
            {
                _remoteCatalog = new LevelCatalog(catalogAsset);
            }

            Debug.Log($"Remote level catalog loaded with {catalogAsset.LevelCount} levels.");
            _hasRemoteLoadFinished = true;
        }
    }
}
