using System;
using BlossomFlow.Application.Events;
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
        [SerializeField] private Image rewardImage;
        [FormerlySerializedAs("restartButton")]
        [SerializeField] private Button claimButton;
        [SerializeField] private Button doubleRewardButton;

        [Header("Copy")]
        [SerializeField] private string title = "Victory";

        [Header("Animation")]
        [SerializeField] private float panelFadeSeconds = 0.24f;
        [SerializeField] private float titleSlideSeconds = 0.36f;
        [SerializeField] private float titleStartOffsetY = 90f;
        [SerializeField] private float rewardAppearSeconds = 0.24f;
        [SerializeField] private float buttonAppearSeconds = 0.22f;
        [SerializeField] private float buttonStaggerSeconds = 0.08f;
        [SerializeField] private Ease titleEase = Ease.OutBack;
        [SerializeField] private Ease appearEase = Ease.OutCubic;

        [Header("State")]
        [SerializeField] private bool hideOnAwake = true;

        private CanvasGroup _canvasGroup;
        private RectTransform _titleRectTransform;
        private Vector2 _titleVisiblePosition;
        private Vector3 _rewardVisibleScale;
        private Vector3 _claimButtonVisibleScale;
        private Vector3 _doubleRewardButtonVisibleScale;
        private Sequence _showSequence;
        private IEventPublisher _eventPublisher;
        private IEventSubscriber _eventSubscriber;
        private IDisposable _victoryScreenRequestedSubscription;
        private IDisposable _levelRestartedSubscription;

        [Inject]
        private void Construct(IEventPublisher eventPublisher, IEventSubscriber eventSubscriber)
        {
            _eventPublisher = eventPublisher;
            _eventSubscriber = eventSubscriber;

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
            ApplyCopy(title);

            if (hideOnAwake)
            {
                Hide();
            }
        }

        private void OnEnable()
        {
            if (claimButton != null)
            {
                claimButton.onClick.AddListener(RequestRestart);
            }

            if (doubleRewardButton != null)
            {
                doubleRewardButton.onClick.AddListener(RequestRestart);
            }

            SubscribeToResolutionEvents();
        }

        private void OnDisable()
        {
            if (claimButton != null)
            {
                claimButton.onClick.RemoveListener(RequestRestart);
            }

            if (doubleRewardButton != null)
            {
                doubleRewardButton.onClick.RemoveListener(RequestRestart);
            }

            KillShowSequence();
            UnsubscribeFromResolutionEvents();
        }

        public void Show()
        {
            ApplyCopy(title);
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

        public void RequestRestart()
        {
            _eventPublisher?.Publish(new LevelRestartRequested());
        }

        private void ApplyCopy(string nextTitle)
        {
            if (titleText != null)
            {
                titleText.text = nextTitle;
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
            AppendAppear(_showSequence, doubleRewardButton != null ? doubleRewardButton.gameObject : null, _doubleRewardButtonVisibleScale, buttonAppearSeconds, buttonStaggerSeconds);

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
        }

        private void UnsubscribeFromResolutionEvents()
        {
            _victoryScreenRequestedSubscription?.Dispose();
            _levelRestartedSubscription?.Dispose();
            _victoryScreenRequestedSubscription = null;
            _levelRestartedSubscription = null;
        }
    }
}
