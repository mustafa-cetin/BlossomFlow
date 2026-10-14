using System;
using BlossomFlow.Application.Ads;
using BlossomFlow.Application.Audio;
using BlossomFlow.Application.Events;
using BlossomFlow.Application.Levels;
using BlossomFlow.Gameplay.Application.Levels;
using BlossomFlow.MainMenu.Domain;
using DG.Tweening;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BlossomFlow.Gameplay.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class VictoryScreenView : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text rewardAmountText;
        [SerializeField] private Image rewardImage;
        [SerializeField] private Sprite magicalSeedRewardSprite;
        [FormerlySerializedAs("restartButton")]
        [SerializeField] private Button claimButton;
        [SerializeField] private Button doubleRewardButton;

        [Header("Copy")]
        [SerializeField] private string title = "Victory";
        [SerializeField] private string rewardAmountFormat = "+{0}";
        [SerializeField] private string magicalSeedRewardAmountFormat = "+{0} magical seed";

        [Header("Animation")]
        [SerializeField] private float panelFadeSeconds = 0.24f;
        [SerializeField] private float titleSlideSeconds = 0.36f;
        [SerializeField] private float titleStartOffsetY = 90f;
        [SerializeField] private float rewardAppearSeconds = 0.24f;
        [SerializeField] private float rewardSwapShrinkSeconds = 0.1f;
        [SerializeField] private float rewardSwapGrowSeconds = 0.16f;
        [SerializeField] private float buttonAppearSeconds = 0.22f;
        [SerializeField] private float buttonStaggerSeconds = 0.08f;
        [SerializeField] private Ease titleEase = Ease.OutBack;
        [SerializeField] private Ease appearEase = Ease.OutCubic;

        [Header("State")]
        [SerializeField] private bool hideOnAwake = true;
        [SerializeField] private int victoryGoldReward = 50;

        private CanvasGroup _canvasGroup;
        private RectTransform _titleRectTransform;
        private Vector2 _titleVisiblePosition;
        private Vector3 _rewardVisibleScale;
        private Vector3 _claimButtonVisibleScale;
        private Vector3 _doubleRewardButtonVisibleScale;
        private Sprite _goldRewardSprite;
        private Sequence _showSequence;
        private Sequence _rewardSwapSequence;
        private IEventPublisher _eventPublisher;
        private IEventSubscriber _eventSubscriber;
        private IMainMenuPlayerProgressService _progressService;
        private IRewardedAdService _rewardedAdService;
        private ICurrentLevelDefinitionProvider _currentLevelDefinitionProvider;
        private ILevelAdvanceService _levelAdvanceService;
        private IDisposable _victoryScreenRequestedSubscription;
        private IDisposable _levelRestartedSubscription;
        private IDisposable _levelAdvancedSubscription;
        private bool _rewardClaimed;
        private bool _goldRewardClaimed;
        private bool _awaitingMagicalSeedClaim;
        private bool _rewardedAdInProgress;
        private int _currentMagicalSeedReward;

        [Inject]
        private void Construct(
            IEventPublisher eventPublisher,
            IEventSubscriber eventSubscriber,
            IMainMenuPlayerProgressService progressService,
            IRewardedAdService rewardedAdService,
            ICurrentLevelDefinitionProvider currentLevelDefinitionProvider,
            ILevelAdvanceService levelAdvanceService)
        {
            _eventPublisher = eventPublisher;
            _eventSubscriber = eventSubscriber;
            _progressService = progressService;
            _rewardedAdService = rewardedAdService;
            _currentLevelDefinitionProvider = currentLevelDefinitionProvider;
            _levelAdvanceService = levelAdvanceService;

            if (isActiveAndEnabled)
            {
                SubscribeToResolutionEvents();
            }
        }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _titleRectTransform = titleText != null ? titleText.rectTransform : null;
            _titleVisiblePosition = _titleRectTransform != null ? _titleRectTransform.anchoredPosition : Vector2.zero;
            _rewardVisibleScale = rewardImage != null ? rewardImage.transform.localScale : Vector3.one;
            _claimButtonVisibleScale = claimButton != null ? claimButton.transform.localScale : Vector3.one;
            _doubleRewardButtonVisibleScale = doubleRewardButton != null ? doubleRewardButton.transform.localScale : Vector3.one;
            _goldRewardSprite = rewardImage != null ? rewardImage.sprite : null;
            EnsureRewardAmountText();
            ApplyCopy(title);
            RefreshGoldRewardPresentation(victoryGoldReward);

            if (hideOnAwake)
            {
                Hide();
            }
        }

        private void OnEnable()
        {
            if (claimButton != null)
            {
                claimButton.onClick.AddListener(ClaimReward);
            }

            if (doubleRewardButton != null)
            {
                doubleRewardButton.onClick.AddListener(ClaimDoubleReward);
            }

            SubscribeToResolutionEvents();
        }

        private void OnDisable()
        {
            if (claimButton != null)
            {
                claimButton.onClick.RemoveListener(ClaimReward);
            }

            if (doubleRewardButton != null)
            {
                doubleRewardButton.onClick.RemoveListener(ClaimDoubleReward);
            }

            KillShowSequence();
            UnsubscribeFromResolutionEvents();
        }

        public void Show()
        {
            _rewardClaimed = false;
            _goldRewardClaimed = false;
            _awaitingMagicalSeedClaim = false;
            _rewardedAdInProgress = false;
            _currentMagicalSeedReward = GetCurrentMagicalSeedReward();
            ApplyCopy(title);
            RefreshGoldRewardPresentation(victoryGoldReward);
            SetButtonsInteractable(true);
            PlayShowAnimation();
        }

        public void Show(string customMessage)
        {
            _ = customMessage;
            Show();
        }

        public void Hide()
        {
            SetVisible(false, true);
        }

        private void ClaimReward()
        {
            PlayButtonClickSound();

            if (_awaitingMagicalSeedClaim)
            {
                GrantMagicalSeedAndLoadNextLevel();
                return;
            }

            if (_currentMagicalSeedReward > 0)
            {
                GrantGoldAndShowMagicalSeedReward(victoryGoldReward);
                return;
            }

            GrantRewardAndLoadNextLevel(victoryGoldReward);
        }

        private void ClaimDoubleReward()
        {
            PlayButtonClickSound();
            if (_rewardClaimed || _rewardedAdInProgress)
            {
                return;
            }

            if (_rewardedAdService == null)
            {
                Debug.LogWarning("Rewarded ad service is not available.");
                return;
            }

            _rewardedAdInProgress = true;
            SetButtonsInteractable(false);
            _rewardedAdService.ShowRewardedAd(
                () => GrantRewardAndLoadNextLevel(victoryGoldReward * 2),
                () =>
                {
                    _rewardedAdInProgress = false;
                    SetButtonsInteractable(true);
                });
        }

        private void GrantRewardAndLoadNextLevel(int goldAmount)
        {
            if (_rewardClaimed)
            {
                return;
            }

            _rewardClaimed = true;
            _rewardedAdInProgress = false;
            SetButtonsInteractable(false);
            _progressService?.AddGoldAndAdvanceLevel(goldAmount);
            AdvanceToSelectedLevel();
        }

        private void GrantGoldAndShowMagicalSeedReward(int goldAmount)
        {
            if (_goldRewardClaimed || _rewardClaimed)
            {
                return;
            }

            _goldRewardClaimed = true;
            _rewardedAdInProgress = false;
            _progressService?.AddGold(goldAmount);
            _awaitingMagicalSeedClaim = true;

            if (claimButton != null)
            {
                claimButton.interactable = false;
            }

            if (doubleRewardButton != null)
            {
                doubleRewardButton.interactable = false;
            }

            PlayMagicalSeedRewardSwap(_currentMagicalSeedReward);
        }

        private void GrantMagicalSeedAndLoadNextLevel()
        {
            if (_rewardClaimed)
            {
                return;
            }

            _rewardClaimed = true;
            _awaitingMagicalSeedClaim = false;
            _rewardedAdInProgress = false;
            SetButtonsInteractable(false);
            _progressService?.AddMagicalSeed(_currentMagicalSeedReward);
            _progressService?.AdvanceLevel();
            AdvanceToSelectedLevel();
        }

        private void OnValidate()
        {
            victoryGoldReward = Mathf.Max(0, victoryGoldReward);
            rewardSwapShrinkSeconds = Mathf.Max(0f, rewardSwapShrinkSeconds);
            rewardSwapGrowSeconds = Mathf.Max(0f, rewardSwapGrowSeconds);
        }

        private void ApplyCopy(string nextTitle)
        {
            if (titleText != null)
            {
                titleText.text = nextTitle;
            }
        }

        private void AdvanceToSelectedLevel()
        {
            if (_levelAdvanceService == null)
            {
                Debug.LogWarning("Level advance service is not available.");
                return;
            }

            var nextLevelNumber = _progressService != null
                ? _progressService.Current.LevelNumber
                : Math.Max(1, (_currentLevelDefinitionProvider?.CurrentLevelNumber ?? 0) + 1);

            _levelAdvanceService.AdvanceToLevel(nextLevelNumber);
        }

        private int GetCurrentMagicalSeedReward()
        {
            if (_currentLevelDefinitionProvider == null ||
                !_currentLevelDefinitionProvider.TryGetCurrentLevelDefinition(out var definition) ||
                definition == null)
            {
                return 0;
            }

            return Mathf.Max(0, definition.MagicalSeedReward);
        }

        private void RefreshGoldRewardPresentation(int goldAmount)
        {
            if (_goldRewardSprite == null && rewardImage != null && rewardImage.sprite != magicalSeedRewardSprite)
            {
                _goldRewardSprite = rewardImage.sprite;
            }

            if (rewardImage != null && _goldRewardSprite != null)
            {
                rewardImage.sprite = _goldRewardSprite;
            }

            RefreshRewardAmount(goldAmount);
        }

        private void RefreshMagicalSeedRewardPresentation(int magicalSeedAmount)
        {
            if (rewardImage != null && magicalSeedRewardSprite != null)
            {
                rewardImage.sprite = magicalSeedRewardSprite;
            }

            RefreshRewardAmount(magicalSeedAmount, magicalSeedRewardAmountFormat);
        }

        private void RefreshRewardAmount(int amount)
        {
            RefreshRewardAmount(amount, rewardAmountFormat);
        }

        private void RefreshRewardAmount(int amount, string format)
        {
            if (rewardAmountText != null)
            {
                format = string.IsNullOrEmpty(format) ? "+{0}" : format;
                rewardAmountText.text = string.Format(format, amount);
            }
        }

        private void PlayMagicalSeedRewardSwap(int magicalSeedAmount)
        {
            if (rewardImage == null)
            {
                RefreshMagicalSeedRewardPresentation(magicalSeedAmount);
                if (claimButton != null)
                {
                    claimButton.interactable = true;
                }

                return;
            }

            KillRewardSwapSequence();
            rewardImage.transform.DOKill();

            var rewardTransform = rewardImage.transform;
            rewardTransform.localScale = _rewardVisibleScale;

            _rewardSwapSequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Append(rewardTransform.DOScale(_rewardVisibleScale * 0.72f, rewardSwapShrinkSeconds)
                    .SetEase(Ease.InCubic))
                .AppendCallback(() => RefreshMagicalSeedRewardPresentation(magicalSeedAmount))
                .Append(rewardTransform.DOScale(_rewardVisibleScale * 1.08f, rewardSwapGrowSeconds)
                    .SetEase(Ease.OutBack))
                .Append(rewardTransform.DOScale(_rewardVisibleScale, rewardSwapGrowSeconds * 0.5f)
                    .SetEase(Ease.OutSine))
                .OnComplete(() =>
                {
                    if (claimButton != null)
                    {
                        claimButton.interactable = true;
                    }
                })
                .OnKill(() => _rewardSwapSequence = null);
        }

        private void EnsureRewardAmountText()
        {
            if (rewardAmountText != null || rewardImage == null)
            {
                return;
            }

            var rewardAmountObject = new GameObject(
                "RewardAmountText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            rewardAmountObject.transform.SetParent(rewardImage.transform, false);

            var rectTransform = rewardAmountObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, -64f);
            rectTransform.sizeDelta = new Vector2(240f, 96f);

            rewardAmountText = rewardAmountObject.GetComponent<TextMeshProUGUI>();
            rewardAmountText.alignment = TextAlignmentOptions.Center;
            rewardAmountText.color = Color.white;
            rewardAmountText.fontSize = 64f;
            rewardAmountText.raycastTarget = false;

            if (titleText != null)
            {
                rewardAmountText.font = titleText.font;
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (claimButton != null)
            {
                claimButton.interactable = interactable;
            }

            if (doubleRewardButton != null)
            {
                doubleRewardButton.interactable = interactable;
            }
        }

        private void PlayShowAnimation()
        {
            KillShowSequence();
            SetVisible(true, false);
            ResetAnimatedElements();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            _showSequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (_canvasGroup != null)
            {
                _showSequence.Append(_canvasGroup.DOFade(1f, panelFadeSeconds)
                    .SetEase(Ease.OutSine));
            }

            if (_titleRectTransform != null)
            {
                _showSequence.Append(_titleRectTransform.DOAnchorPos(_titleVisiblePosition, titleSlideSeconds)
                    .SetEase(titleEase));
            }

            AppendAppear(_showSequence, rewardImage != null ? rewardImage.gameObject : null, _rewardVisibleScale, rewardAppearSeconds, 0f);
            AppendAppear(_showSequence, claimButton != null ? claimButton.gameObject : null, _claimButtonVisibleScale, buttonAppearSeconds, 0f);
            //AppendAppear(_showSequence, doubleRewardButton != null ? doubleRewardButton.gameObject : null, _doubleRewardButtonVisibleScale, buttonAppearSeconds, buttonStaggerSeconds);

            _showSequence.OnComplete(() =>
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.interactable = true;
                    _canvasGroup.blocksRaycasts = true;
                }
            });
        }

        private void SetVisible(bool visible, bool resetAnimatedElements)
        {
            KillShowSequence();

            if (panelRoot != null && panelRoot != gameObject)
            {
                panelRoot.SetActive(visible);
            }

            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;

            if (resetAnimatedElements)
            {
                ResetAnimatedElements();
            }
        }

        private void ResetAnimatedElements()
        {
            if (_titleRectTransform != null)
            {
                _titleRectTransform.DOKill();
                _titleRectTransform.anchoredPosition = _titleVisiblePosition + Vector2.up * titleStartOffsetY;
            }

            ResetAppearTarget(rewardImage != null ? rewardImage.gameObject : null, _rewardVisibleScale);
            ResetAppearTarget(claimButton != null ? claimButton.gameObject : null, _claimButtonVisibleScale);
            ResetAppearTarget(doubleRewardButton != null ? doubleRewardButton.gameObject : null, _doubleRewardButtonVisibleScale);
        }

        private static void ResetAppearTarget(GameObject target, Vector3 visibleScale)
        {
            if (target == null)
            {
                return;
            }

            target.transform.DOKill();
            target.transform.localScale = visibleScale * 0.88f;
            target.SetActive(false);

            var canvasGroup = GetOrAddCanvasGroup(target);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void AppendAppear(Sequence sequence, GameObject target, Vector3 visibleScale, float appearSeconds, float delay)
        {
            if (sequence == null || target == null)
            {
                return;
            }

            sequence.AppendInterval(delay);
            sequence.AppendCallback(() => target.SetActive(true));

            var canvasGroup = GetOrAddCanvasGroup(target);
            sequence.Join(canvasGroup.DOFade(1f, appearSeconds)
                .SetEase(Ease.OutSine));
            sequence.Join(target.transform.DOScale(visibleScale, appearSeconds)
                .SetEase(appearEase));
            sequence.AppendCallback(() =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            });
        }

        private void KillShowSequence()
        {
            _showSequence?.Kill();
            _showSequence = null;
            KillRewardSwapSequence();
        }

        private void KillRewardSwapSequence()
        {
            _rewardSwapSequence?.Kill();
            _rewardSwapSequence = null;
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            if (target.TryGetComponent(out CanvasGroup canvasGroup))
            {
                return canvasGroup;
            }

            return target.AddComponent<CanvasGroup>();
        }

        private void SubscribeToResolutionEvents()
        {
            if (_eventSubscriber == null || _victoryScreenRequestedSubscription != null)
            {
                return;
            }

            _victoryScreenRequestedSubscription = _eventSubscriber.Subscribe<LevelVictoryScreenRequested>(_ => Show());
            _levelRestartedSubscription = _eventSubscriber.Subscribe<LevelRestarted>(_ => Hide());
            _levelAdvancedSubscription = _eventSubscriber.Subscribe<LevelAdvanced>(_ => Hide());
        }

        private void UnsubscribeFromResolutionEvents()
        {
            _victoryScreenRequestedSubscription?.Dispose();
            _levelRestartedSubscription?.Dispose();
            _levelAdvancedSubscription?.Dispose();
            _victoryScreenRequestedSubscription = null;
            _levelRestartedSubscription = null;
            _levelAdvancedSubscription = null;
        }

        private void PlayButtonClickSound()
        {
            _eventPublisher?.Publish(new SoundRequested(SoundCueId.ButtonClick));
        }
    }
}
