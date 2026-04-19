using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MainMenuGardenPlotView : MonoBehaviour
    {
        [SerializeField] private Button plantButton;
        [SerializeField] private Image sproutImage;
        [SerializeField] private Image flowerImage;
        [SerializeField] private TMP_Text growthCountdownText;
        [SerializeField] private TMP_Text plantButtonText;
        [SerializeField] private string plantCopy = "Ek";
        [SerializeField, Min(0f)] private float bloomTransitionSeconds = 0.35f;
        [SerializeField] private Ease bloomTransitionEase = Ease.OutCubic;

        private Action<int> _plantRequested;
        private int _regionIndex;
        private bool _hasState;
        private bool _wasGrowing;
        private bool _wasGrown;
        private Sequence _bloomSequence;

        public void Initialize(int regionIndex, Action<int> plantRequested)
        {
            _regionIndex = regionIndex;
            _plantRequested = plantRequested;
            ApplyCopy();
        }

        public void SetState(bool isPlanted, bool isGrown, string growthCountdown, bool canPlant)
        {
            var isGrowing = isPlanted && !isGrown;
            var shouldPlayBloomTransition = _hasState && _wasGrowing && isGrown && !_wasGrown;

            if (shouldPlayBloomTransition)
            {
                PlayBloomTransition();
            }
            else if (_bloomSequence != null && isPlanted && isGrown && _wasGrown)
            {
                // Keep the live bloom transition running across countdown refresh ticks.
            }
            else
            {
                KillBloomTransition();
                ApplyPlantVisuals(isGrowing, isPlanted && isGrown);
            }

            if (growthCountdownText != null)
            {
                growthCountdownText.text = growthCountdown ?? string.Empty;
                growthCountdownText.gameObject.SetActive(isGrowing && !string.IsNullOrWhiteSpace(growthCountdown));
            }

            if (plantButton != null)
            {
                plantButton.gameObject.SetActive(!isPlanted && canPlant);
                plantButton.interactable = !isPlanted && canPlant;
            }

            _hasState = true;
            _wasGrowing = isGrowing;
            _wasGrown = isPlanted && isGrown;
        }

        private void Awake()
        {
            if (plantButton == null)
            {
                plantButton = GetComponentInChildren<Button>(true);
            }
        }

        private void OnEnable()
        {
            if (plantButton != null)
            {
                plantButton.onClick.AddListener(RequestPlant);
            }

            ApplyCopy();
        }

        private void OnDisable()
        {
            KillBloomTransition();

            if (plantButton != null)
            {
                plantButton.onClick.RemoveListener(RequestPlant);
            }
        }

        private void OnValidate()
        {
            ApplyCopy();
        }

        private void RequestPlant()
        {
            _plantRequested?.Invoke(_regionIndex);
        }

        private void ApplyPlantVisuals(bool showSprout, bool showFlower)
        {
            if (sproutImage != null)
            {
                sproutImage.DOKill();
                SetImageAlpha(sproutImage, 1f);
                sproutImage.gameObject.SetActive(showSprout);
            }

            if (flowerImage != null)
            {
                flowerImage.DOKill();
                SetImageAlpha(flowerImage, 1f);
                flowerImage.gameObject.SetActive(showFlower);
            }
        }

        private void PlayBloomTransition()
        {
            KillBloomTransition();

            if (sproutImage == null || flowerImage == null || bloomTransitionSeconds <= 0f)
            {
                ApplyPlantVisuals(false, true);
                return;
            }

            sproutImage.gameObject.SetActive(true);
            flowerImage.gameObject.SetActive(true);
            SetImageAlpha(sproutImage, 1f);
            SetImageAlpha(flowerImage, 0f);

            _bloomSequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            _bloomSequence.Join(sproutImage.DOFade(0f, bloomTransitionSeconds).SetEase(bloomTransitionEase));
            _bloomSequence.Join(flowerImage.DOFade(1f, bloomTransitionSeconds).SetEase(bloomTransitionEase));
            _bloomSequence.OnComplete(() =>
            {
                if (sproutImage != null)
                {
                    sproutImage.gameObject.SetActive(false);
                    SetImageAlpha(sproutImage, 1f);
                }

                if (flowerImage != null)
                {
                    flowerImage.gameObject.SetActive(true);
                    SetImageAlpha(flowerImage, 1f);
                }

                _bloomSequence = null;
            });
        }

        private void KillBloomTransition()
        {
            _bloomSequence?.Kill();
            _bloomSequence = null;
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private void ApplyCopy()
        {
            if (plantButtonText != null)
            {
                plantButtonText.text = plantCopy;
            }
        }
    }
}
