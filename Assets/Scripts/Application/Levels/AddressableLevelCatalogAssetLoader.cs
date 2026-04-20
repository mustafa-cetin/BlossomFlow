using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BlossomFlow.Application.Levels
{
    public sealed class AddressableLevelCatalogAssetLoader : ILevelCatalogAssetLoader
    {
        private readonly AssetReferenceT<LevelCatalogAsset> _catalogReference;
        private readonly bool _updateCatalogsBeforeLoad;

        public AddressableLevelCatalogAssetLoader(
            AssetReferenceT<LevelCatalogAsset> catalogReference,
            bool updateCatalogsBeforeLoad)
        {
            _catalogReference = catalogReference;
            _updateCatalogsBeforeLoad = updateCatalogsBeforeLoad;
        }

        public IDisposable Load(Action<LevelCatalogAsset> onLoaded, Action<Exception> onFailed)
        {
            if (_catalogReference == null || !_catalogReference.RuntimeKeyIsValid())
            {
                onFailed?.Invoke(new InvalidOperationException("Remote level catalog Addressables reference is missing or invalid."));
                return DisposableAction.Empty;
            }

            var request = new LoadRequest(_catalogReference, _updateCatalogsBeforeLoad, onLoaded, onFailed);
            request.Start();
            return request;
        }

        private sealed class LoadRequest : IDisposable
        {
            private readonly AssetReferenceT<LevelCatalogAsset> _catalogReference;
            private readonly bool _updateCatalogsBeforeLoad;
            private Action<LevelCatalogAsset> _onLoaded;
            private Action<Exception> _onFailed;
            private AsyncOperationHandle<IResourceLocator> _initializeHandle;
            private AsyncOperationHandle<List<string>> _catalogCheckHandle;
            private AsyncOperationHandle<List<IResourceLocator>> _catalogUpdateHandle;
            private AsyncOperationHandle<LevelCatalogAsset> _catalogLoadHandle;
            private bool _isDisposed;
            private bool _isComplete;

            public LoadRequest(
                AssetReferenceT<LevelCatalogAsset> catalogReference,
                bool updateCatalogsBeforeLoad,
                Action<LevelCatalogAsset> onLoaded,
                Action<Exception> onFailed)
            {
                _catalogReference = catalogReference;
                _updateCatalogsBeforeLoad = updateCatalogsBeforeLoad;
                _onLoaded = onLoaded;
                _onFailed = onFailed;
            }

            public void Start()
            {
                try
                {
                    _initializeHandle = Addressables.InitializeAsync(false);
                    _initializeHandle.Completed += OnAddressablesInitialized;
                }
                catch (Exception exception)
                {
                    Fail(exception);
                }
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
                ReleaseHandles();
            }

            private void OnAddressablesInitialized(AsyncOperationHandle<IResourceLocator> handle)
            {
                if (_isDisposed)
                {
                    return;
                }

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Fail(handle.OperationException);
                    return;
                }

                if (_updateCatalogsBeforeLoad)
                {
                    try
                    {
                        _catalogCheckHandle = Addressables.CheckForCatalogUpdates(false);
                        _catalogCheckHandle.Completed += OnCatalogUpdateCheckCompleted;
                    }
                    catch (Exception exception)
                    {
                        Fail(exception);
                    }

                    return;
                }

                LoadCatalogAsset();
            }

            private void OnCatalogUpdateCheckCompleted(AsyncOperationHandle<List<string>> handle)
            {
                if (_isDisposed)
                {
                    return;
                }

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Fail(handle.OperationException);
                    return;
                }

                if (handle.Result == null || handle.Result.Count == 0)
                {
                    LoadCatalogAsset();
                    return;
                }

                try
                {
                    _catalogUpdateHandle = Addressables.UpdateCatalogs(new List<string>(handle.Result), false);
                    _catalogUpdateHandle.Completed += OnCatalogUpdateCompleted;
                }
                catch (Exception exception)
                {
                    Fail(exception);
                }
            }

            private void OnCatalogUpdateCompleted(AsyncOperationHandle<List<IResourceLocator>> handle)
            {
                if (_isDisposed)
                {
                    return;
                }

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Fail(handle.OperationException);
                    return;
                }

                LoadCatalogAsset();
            }

            private void LoadCatalogAsset()
            {
                try
                {
                    _catalogLoadHandle = _catalogReference.LoadAssetAsync();
                    _catalogLoadHandle.Completed += OnCatalogLoaded;
                }
                catch (Exception exception)
                {
                    Fail(exception);
                }
            }

            private void OnCatalogLoaded(AsyncOperationHandle<LevelCatalogAsset> handle)
            {
                if (_isDisposed)
                {
                    return;
                }

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Fail(handle.OperationException);
                    return;
                }

                Complete(handle.Result);
            }

            private void Fail(Exception exception)
            {
                if (_isComplete)
                {
                    return;
                }

                _isComplete = true;

                if (exception != null)
                {
                    Debug.LogWarning($"Remote level catalog load failed. Local catalog will be used. {exception.Message}");
                }

                _onFailed?.Invoke(exception);
                ReleaseFailedHandles();
            }

            private void Complete(LevelCatalogAsset catalogAsset)
            {
                if (_isComplete)
                {
                    return;
                }

                _isComplete = true;
                _onLoaded?.Invoke(catalogAsset);
                ReleaseTransientHandles();
            }

            private void ReleaseHandles()
            {
                ReleaseHandle(ref _catalogLoadHandle);
                ReleaseTransientHandles();
            }

            private void ReleaseFailedHandles()
            {
                ReleaseHandle(ref _catalogLoadHandle);
                ReleaseTransientHandles();
            }

            private void ReleaseTransientHandles()
            {
                ReleaseHandle(ref _catalogUpdateHandle);
                ReleaseHandle(ref _catalogCheckHandle);
                ReleaseHandle(ref _initializeHandle);
            }

            private static void ReleaseHandle<T>(ref AsyncOperationHandle<T> handle)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                handle = default;
            }
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
