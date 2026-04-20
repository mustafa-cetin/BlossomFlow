using System;
using System.Collections;
using BlossomFlow.Application.Events;
using BlossomFlow.Gameplay.Domain.Levels;
using UnityEngine;

namespace BlossomFlow.Application.Levels
{
    public sealed class RemoteLevelCatalog : ILevelCatalog, IRemoteLevelCatalogPreloader, IDisposable
    {
        private const float MinimumPreloadTimeoutSeconds = 1f;

        private readonly ILevelCatalogAssetLoader _remoteCatalogLoader;
        private readonly IEventPublisher _eventPublisher;
        private readonly float _preloadTimeoutSeconds;
        private readonly object _syncRoot = new object();
        private IDisposable _activeLoad;
        private LevelCatalog _remoteCatalog;
        private bool _hasRemoteLoadFinished;
        private bool _hasRemoteLoadStarted;
        private bool _isUnavailable;
        private string _unavailableReason;
        private bool _isDisposed;

        public RemoteLevelCatalog(
            ILevelCatalogAssetLoader remoteCatalogLoader,
            IEventPublisher eventPublisher,
            float preloadTimeoutSeconds)
        {
            _remoteCatalogLoader = remoteCatalogLoader;
            _eventPublisher = eventPublisher;
            _preloadTimeoutSeconds = Mathf.Max(MinimumPreloadTimeoutSeconds, preloadTimeoutSeconds);
            EnsureRemoteLoadStarted();
        }

        public bool IsUnavailable => _isUnavailable;

        public bool IsLoaded => _remoteCatalog != null;

        public string UnavailableReason => _unavailableReason;

        public int LevelCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _remoteCatalog != null ? _remoteCatalog.LevelCount : 0;
                }
            }
        }

        public bool TryGetDefinition(int levelNumber, out LevelDefinition definition)
        {
            definition = null;
            EnsureRemoteLoadStarted();

            lock (_syncRoot)
            {
                return _remoteCatalog != null && _remoteCatalog.TryGetDefinition(levelNumber, out definition);
            }
        }

        public IDisposable LoadDefinition(int levelNumber, Action<LevelDefinition> onLoaded, Action<Exception> onFailed)
        {
            EnsureRemoteLoadStarted();

            lock (_syncRoot)
            {
                if (_remoteCatalog != null)
                {
                    return _remoteCatalog.LoadDefinition(levelNumber, onLoaded, onFailed);
                }
            }

            onFailed?.Invoke(new InvalidOperationException("Remote level catalog is not available."));
            return DisposableAction.Empty;
        }

        public IEnumerator LoadIfNeeded()
        {
            EnsureRemoteLoadStarted();

            var startedAt = Time.realtimeSinceStartup;
            while (!_hasRemoteLoadFinished)
            {
                if (Time.realtimeSinceStartup - startedAt >= _preloadTimeoutSeconds)
                {
                    MarkUnavailable($"Remote level catalog load timed out after {_preloadTimeoutSeconds:0.#} seconds.");
                    yield break;
                }

                yield return null;
            }
        }

        public void RetryLoad()
        {
            if (_isDisposed || _remoteCatalog != null)
            {
                return;
            }

            _activeLoad?.Dispose();
            _activeLoad = null;
            _hasRemoteLoadFinished = false;
            _hasRemoteLoadStarted = false;
            _isUnavailable = false;
            _unavailableReason = null;
            EnsureRemoteLoadStarted();
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
            _remoteCatalog?.Dispose();
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
                MarkUnavailable("Remote level catalog loader is not registered.");
                return;
            }

            _activeLoad = _remoteCatalogLoader.Load(UseRemoteCatalog, OnRemoteCatalogFailed);
        }

        private void UseRemoteCatalog(LevelCatalogAsset catalogAsset)
        {
            if (_isDisposed || catalogAsset == null)
            {
                MarkUnavailable("Remote level catalog reference did not resolve to a catalog asset.");
                return;
            }

            lock (_syncRoot)
            {
                _remoteCatalog = new LevelCatalog(catalogAsset);
            }

            _isUnavailable = false;
            _unavailableReason = null;
            Debug.Log($"Remote level catalog loaded with {catalogAsset.LevelCount} levels.");
            _hasRemoteLoadFinished = true;
        }

        private void OnRemoteCatalogFailed(Exception exception)
        {
            var reason = exception != null
                ? $"Remote level catalog load failed: {exception.Message}"
                : "Remote level catalog load failed.";

            MarkUnavailable(reason);
        }

        private void MarkUnavailable(string reason)
        {
            if (_hasRemoteLoadFinished)
            {
                return;
            }

            _hasRemoteLoadFinished = true;
            _activeLoad?.Dispose();
            _activeLoad = null;
            _isUnavailable = true;
            _unavailableReason = reason;

            Debug.LogError($"{reason} No local level catalog fallback is configured.");
            _eventPublisher?.Publish(new RemoteLevelCatalogUnavailable(reason));
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
