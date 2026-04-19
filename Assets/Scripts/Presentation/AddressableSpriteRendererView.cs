using System;
using BlossomFlow.Application.Sprites;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BlossomFlow.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AddressableSpriteRendererView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private AssetReferenceT<Sprite> spriteReference;
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
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            LoadSprite();
        }

        private void OnDisable()
        {
            ReleaseActiveLoad();

            if (clearSpriteOnDisable && targetRenderer != null)
            {
                targetRenderer.sprite = null;
            }
        }

        private void OnDestroy()
        {
            ReleaseActiveLoad();
        }

        private void LoadSprite()
        {
            if (_spriteLoader == null || targetRenderer == null)
            {
                return;
            }

            ReleaseActiveLoad();
            var requestVersion = ++_loadVersion;

            _activeLoad = _spriteLoader.Load(spriteReference, sprite =>
            {
                if (requestVersion != _loadVersion || targetRenderer == null)
                {
                    return;
                }

                targetRenderer.sprite = sprite;
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
