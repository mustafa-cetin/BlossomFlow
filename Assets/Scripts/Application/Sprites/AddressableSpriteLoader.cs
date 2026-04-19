using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BlossomFlow.Application.Sprites
{
    public sealed class AddressableSpriteLoader : IAddressableSpriteLoader
    {
        public IDisposable Load(AssetReferenceT<Sprite> spriteReference, Action<Sprite> onLoaded)
        {
            if (spriteReference == null || !spriteReference.RuntimeKeyIsValid())
            {
                onLoaded?.Invoke(null);
                return DisposableAction.Empty;
            }

            var handle = spriteReference.LoadAssetAsync();
            return new SpriteLoadRequest(handle, onLoaded);
        }

        private sealed class SpriteLoadRequest : IDisposable
        {
            private AsyncOperationHandle<Sprite> _handle;
            private Action<Sprite> _onLoaded;
            private bool _isDisposed;

            public SpriteLoadRequest(AsyncOperationHandle<Sprite> handle, Action<Sprite> onLoaded)
            {
                _handle = handle;
                _onLoaded = onLoaded;
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

                if (_handle.IsValid())
                {
                    _handle.Completed -= OnCompleted;
                    Addressables.Release(_handle);
                }
            }

            private void OnCompleted(AsyncOperationHandle<Sprite> handle)
            {
                if (_isDisposed)
                {
                    return;
                }

                if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
                {
                    _onLoaded?.Invoke(null);
                    return;
                }

                _onLoaded?.Invoke(handle.Result);
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
