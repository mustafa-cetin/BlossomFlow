using DG.Tweening;
using UnityEngine;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MainMenuAmbientArtworkView : MonoBehaviour
    {
        [SerializeField] private RectTransform artworkRoot;
        [SerializeField] private float swayDegrees = 1.5f;
        [SerializeField] private float floatOffsetY = 8f;
        [SerializeField] private float cycleSeconds = 2.4f;
        [SerializeField] private Ease ease = Ease.InOutSine;

        private Sequence _ambientSequence;
        private Vector2 _initialAnchoredPosition;
        private Vector3 _initialEulerAngles;

        private void Awake()
        {
            if (artworkRoot == null)
            {
                artworkRoot = transform as RectTransform;
            }

            if (artworkRoot == null)
            {
                return;
            }

            _initialAnchoredPosition = artworkRoot.anchoredPosition;
            _initialEulerAngles = artworkRoot.localEulerAngles;
        }

        private void OnEnable()
        {
            StartAmbientAnimation();
        }

        private void OnDisable()
        {
            StopAmbientAnimation();
        }

        private void StartAmbientAnimation()
        {
            if (artworkRoot == null)
            {
                return;
            }

            StopAmbientAnimation();
            artworkRoot.anchoredPosition = _initialAnchoredPosition;
            artworkRoot.localEulerAngles = _initialEulerAngles;

            _ambientSequence = DOTween.Sequence()
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(artworkRoot.gameObject, LinkBehaviour.KillOnDestroy);

            _ambientSequence.Join(artworkRoot.DOAnchorPosY(_initialAnchoredPosition.y + floatOffsetY, cycleSeconds).SetEase(ease));
            _ambientSequence.Join(artworkRoot.DOLocalRotate(_initialEulerAngles + Vector3.forward * swayDegrees, cycleSeconds).SetEase(ease));
        }

        private void StopAmbientAnimation()
        {
            _ambientSequence?.Kill();
            _ambientSequence = null;

            if (artworkRoot == null)
            {
                return;
            }

            artworkRoot.anchoredPosition = _initialAnchoredPosition;
            artworkRoot.localEulerAngles = _initialEulerAngles;
        }
    }
}
