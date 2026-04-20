using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BlossomFlow.MainMenu.Presentation
{
    public sealed class MainMenuGardenTutorialView : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject handPrefab;
        [SerializeField] private TextMeshProUGUI textPrefab;
        [SerializeField] private Canvas canvas;

        [Header("Layout")]
        [SerializeField] private Vector3 handOffset = new(0.28f, 0.05f, 0f);
        [SerializeField] private Vector3 labelOffset = new(0f, 0.56f, 0f);
        [SerializeField] private Vector3 centerMessageOffset = Vector3.zero;
        [SerializeField, Min(0f)] private float handBobDistance = 0.18f;
        [SerializeField, Min(0.01f)] private float handBobSeconds = 0.55f;

        private Transform _hand;
        private TextMeshProUGUI _label;
        private Sequence _pulseSequence;

        public void PointAt(Vector3 worldPosition, string text)
        {
            EnsureVisuals();
            if (_hand == null && _label == null)
            {
                return;
            }

            KillPulse();

            if (_hand != null)
            {
                _hand.gameObject.SetActive(true);
                var handPosition = worldPosition + handOffset;
                _hand.position = handPosition;

                _pulseSequence = DOTween.Sequence()
                    .SetLink(_hand.gameObject, LinkBehaviour.KillOnDestroy)
                    .SetUpdate(true)
                    .SetLoops(-1, LoopType.Yoyo);

                _pulseSequence.Append(_hand.DOMove(handPosition + Vector3.up * handBobDistance, handBobSeconds).SetEase(Ease.InOutSine));
            }

            if (_label != null)
            {
                _label.gameObject.SetActive(true);
                _label.transform.position = worldPosition + labelOffset;
                _label.text = text ?? string.Empty;
            }
        }

        public void Hide()
        {
            KillPulse();

            if (_hand != null)
            {
                _hand.gameObject.SetActive(false);
            }

            if (_label != null)
            {
                _label.gameObject.SetActive(false);
            }
        }

        public void ShowCenterMessage(string text)
        {
            EnsureVisuals();
            KillPulse();

            if (_hand != null)
            {
                _hand.gameObject.SetActive(false);
            }

            if (_label == null)
            {
                return;
            }

            _label.gameObject.SetActive(true);
            _label.transform.position = GetCenterPosition() + centerMessageOffset;
            _label.text = text ?? string.Empty;
        }

        private void OnDestroy()
        {
            KillPulse();
        }

        private void EnsureVisuals()
        {
            var parent = canvas != null ? canvas.transform : transform;
            if (_hand == null && handPrefab != null)
            {
                _hand = Instantiate(handPrefab, parent).transform;
                _hand.gameObject.SetActive(false);
            }

            if (_label == null && textPrefab != null)
            {
                _label = Instantiate(textPrefab, parent);
                _label.gameObject.SetActive(false);
            }
        }

        private void KillPulse()
        {
            _pulseSequence?.Kill();
            _pulseSequence = null;
        }

        private Vector3 GetCenterPosition()
        {
            if (canvas == null)
            {
                return transform.position;
            }

            var rectTransform = canvas.transform as RectTransform;
            return rectTransform != null
                ? rectTransform.TransformPoint(rectTransform.rect.center)
                : canvas.transform.position;
        }
    }
}
