using UnityEngine;
using UnityEngine.UI;

namespace BlossomFlow.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class AddressableImageView : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite sprite;
        [SerializeField] private bool preserveNativeSize;
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
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }
        }

        private void OnEnable()
        {
            ApplySprite();
        }

        private void OnDisable()
        {
            if (clearSpriteOnDisable && targetImage != null)
            {
                targetImage.sprite = null;
                targetImage.enabled = false;
            }
        }

        private void ApplySprite()
        {
            if (targetImage == null)
            {
                return;
            }

            targetImage.sprite = sprite;
            targetImage.enabled = sprite != null;

            if (sprite != null && preserveNativeSize)
            {
                targetImage.SetNativeSize();
            }
        }
    }
}
