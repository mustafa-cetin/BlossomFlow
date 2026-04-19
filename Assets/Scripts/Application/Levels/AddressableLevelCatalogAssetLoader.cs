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
                onLoaded?.Invoke(null);
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
                _initializeHandle = Addressables.InitializeAsync(false);
                _initializeHandle.Completed += OnAddressablesInitialized;
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
                    _catalogCheckHandle = Addressables.CheckForCatalogUpdates(false);
                    _catalogCheckHandle.Completed += OnCatalogUpdateCheckCompleted;
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

                _catalogUpdateHandle = Addressables.UpdateCatalogs(new List<string>(handle.Result), false);
                _catalogUpdateHandle.Completed += OnCatalogUpdateCompleted;
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
                _catalogLoadHandle = _catalogReference.LoadAssetAsync();
                _catalogLoadHandle.Completed += OnCatalogLoaded;
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

                _onLoaded?.Invoke(handle.Result);
            }

            private void Fail(Exception exception)
            {
                if (exception != null)
                {
                    Debug.LogWarning($"Remote level catalog load failed. Local catalog will be used. {exception.Message}");
                }

                _onFailed?.Invoke(exception);
            }

            private void ReleaseHandles()
            {
                ReleaseHandle(ref _catalogLoadHandle);
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
