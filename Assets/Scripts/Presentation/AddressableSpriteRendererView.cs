using UnityEngine;

namespace BlossomFlow.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AddressableSpriteRendererView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite sprite;
        [SerializeField] private bool clearSpriteOnDisable = true;

        public void SetSprite(Sprite nextSprite)
        {
            sprite = nextSprite;

            if (isActiveAndEnabled)
            {
                ApplySprite();
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
            ApplySprite();
        }

        private void OnDisable()
        {
            if (clearSpriteOnDisable && targetRenderer != null)
            {
                targetRenderer.sprite = null;
            }
        }

        private void ApplySprite()
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.sprite = sprite;
        }
    }
}
