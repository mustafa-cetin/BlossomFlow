using System;
using BlossomFlow.Application.Sprites;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace BlossomFlow.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class AddressableImageView : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private AssetReferenceT<Sprite> spriteReference;
        [SerializeField] private bool preserveNativeSize;
        [SerializeField] private bool clearSpriteOnDisable = true;

        private IAddressableSpriteLoader _spriteLoader;
        private IDisposable _activeLoad;
        private int _loadVersion;

        [Inject]
        private void Construct(IAddressableSpriteLoader spriteLoader)
        {
            _spriteLoader = spriteLoader;

            if (isActiveAndEnabled)
            {
                LoadSprite();
            }
        }

        public void SetSpriteReference(AssetReferenceT<Sprite> nextSpriteReference)
        {
            spriteReference = nextSpriteReference;

            if (isActiveAndEnabled)
            {
                LoadSprite();
            }
        }

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }
        }

        private void OnEnable()
        {
            LoadSprite();
        }

        private void OnDisable()
        {
            ReleaseActiveLoad();

            if (clearSpriteOnDisable && targetImage != null)
            {
                targetImage.sprite = null;
                targetImage.enabled = false;
            }
        }

        private void OnDestroy()
        {
            ReleaseActiveLoad();
        }

        private void LoadSprite()
        {
            if (_spriteLoader == null || targetImage == null)
            {
                return;
            }

            ReleaseActiveLoad();
            var requestVersion = ++_loadVersion;
            targetImage.enabled = false;

            _activeLoad = _spriteLoader.Load(spriteReference, sprite =>
            {
                if (requestVersion != _loadVersion || targetImage == null)
                {
                    return;
                }

                targetImage.sprite = sprite;
                targetImage.enabled = sprite != null;

                if (sprite != null && preserveNativeSize)
                {
                    targetImage.SetNativeSize();
                }
            });
        }

        private void ReleaseActiveLoad()
        {
            _loadVersion++;
            _activeLoad?.Dispose();
            _activeLoad = null;
        }
    }
}
