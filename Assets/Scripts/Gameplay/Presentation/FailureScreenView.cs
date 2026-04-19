using System;
using BlossomFlow.Application.Audio;
using BlossomFlow.Application.Events;
using BlossomFlow.Application.SceneLoading;
using BlossomFlow.MainMenu.Domain;
using DG.Tweening;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlossomFlow.Gameplay.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class FailureScreenView : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text primaryButtonText;
        [SerializeField] private Image primaryButtonCurrencyIcon;
        [SerializeField] private Image artworkImage;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Button closeButton;

        [Header("Artwork")]
        [SerializeField] private Sprite keepPlayingSprite;
        [SerializeField] private Sprite confirmQuitSprite;
        [SerializeField] private Sprite retrySprite;

        [Header("Copy")]
        [SerializeField] private string keepPlayingMessage = "Keep Playing";
        [SerializeField] private string confirmQuitMessage = "Are you sure?";
        [SerializeField] private string retryMessage = "Try Again?";
        [SerializeField] private string keepPlayingButtonFormat = "Play On {0}";
        [SerializeField] private string retryButtonText = "Try Again";

        [Header("Cost")]
        [SerializeField] private int keepPlayingGoldCost = 900;
        [SerializeField] private bool hideOnAwake = true;
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";

        [Header("Animation")]
        [SerializeField] private float artworkPopScale = 1.12f;
        [SerializeField] private float artworkShrinkSeconds = 0.08f;
        [SerializeField] private float artworkGrowSeconds = 0.16f;
        [SerializeField] private Ease artworkShrinkEase = Ease.InSine;
        [SerializeField] private Ease artworkGrowEase = Ease.OutBack;

        private CanvasGroup _canvasGroup;
        private IEventPublisher _eventPublisher;
        private IEventSubscriber _eventSubscriber;
        private IMainMenuPlayerProgressService _progressService;
        private ISceneTransitionService _sceneTransitionService;
        private IDisposable _failureScreenRequestedSubscription;
        private IDisposable _keepPlayingStartedSubscription;
        private IDisposable _levelRestartedSubscription;
        private IDisposable _progressChangedSubscription;
        private FailureScreenState _state;
        private Sprite _currentArtworkSprite;
        private Vector3 _artworkBaseScale = Vector3.one;
        private Sequence _artworkSequence;

        [Inject]
        private void Construct(
            IEventPublisher eventPublisher,
            IEventSubscriber eventSubscriber,
            IMainMenuPlayerProgressService progressService,
            ISceneTransitionService sceneTransitionService)
        {
            _eventPublisher = eventPublisher;
            _eventSubscriber = eventSubscriber;
            _progressService = progressService;
            _sceneTransitionService = sceneTransitionService;

            if (isActiveAndEnabled)
            {
                SubscribeToEvents();
            }
        }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _artworkBaseScale = artworkImage != null ? artworkImage.transform.localScale : Vector3.one;

            if (hideOnAwake)
            {
                Hide();
            }
        }

        private void OnEnable()
        {
            if (primaryButton != null)
            {
                primaryButton.onClick.AddListener(OnPrimaryClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
            }

            SubscribeToEvents();
            Refresh();
        }

        private void OnDisable()
        {
            if (primaryButton != null)
            {
                primaryButton.onClick.RemoveListener(OnPrimaryClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseClicked);
            }

            KillArtworkSequence();
            UnsubscribeFromEvents();
        }

        private void OnValidate()
        {
            keepPlayingGoldCost = Mathf.Max(0, keepPlayingGoldCost);
            artworkPopScale = Mathf.Max(1f, artworkPopScale);
            artworkShrinkSeconds = Mathf.Max(0f, artworkShrinkSeconds);
            artworkGrowSeconds = Mathf.Max(0f, artworkGrowSeconds);
        }

        private void ShowKeepPlayingOffer()
        {
            _state = FailureScreenState.KeepPlayingOffer;
            SetVisible(true);
            Refresh();
        }

        private void Hide()
        {
            SetVisible(false);
        }

        private void OnPrimaryClicked()
        {
            PlayButtonClickSound();
            if (_state == FailureScreenState.RetryOnly)
            {
                TryRestartLevel();
                return;
            }

            TryKeepPlaying();
        }

        private void OnCloseClicked()
        {
            PlayButtonClickSound();
            if (_state == FailureScreenState.KeepPlayingOffer)
            {
                _state = FailureScreenState.ConfirmQuit;
                Refresh();
                return;
            }

            if (_state == FailureScreenState.ConfirmQuit)
            {
                _state = FailureScreenState.RetryOnly;
                Refresh();
                return;
            }

            ReturnToMainMenu();
        }

        private void TryKeepPlaying()
        {
            if (_progressService == null || _eventPublisher == null)
            {
                return;
            }

            if (!_progressService.TrySpendGold(keepPlayingGoldCost))
            {
                Refresh();
                return;
            }

            _eventPublisher.Publish(new LevelKeepPlayingRequested());
        }

        private void TryRestartLevel()
        {
            if (_progressService == null || _eventPublisher == null)
            {
                return;
            }

            if (!_progressService.TryConsumeLife())
            {
                Refresh();
                return;
            }

            _eventPublisher.Publish(new LevelRestartRequested());
        }

        private void Refresh()
        {
            ApplyStateContent();
            RefreshButtonState();
        }

        private void ApplyStateContent()
        {
            if (messageText != null)
            {
                messageText.text = GetMessage();
            }

            if (primaryButtonText != null)
            {
                primaryButtonText.text = GetPrimaryButtonLabel();
            }

            if (primaryButtonCurrencyIcon != null)
            {
                primaryButtonCurrencyIcon.gameObject.SetActive(ShouldShowPrimaryButtonCurrencyIcon());
            }

            if (artworkImage != null)
            {
                ApplyArtworkSprite(GetArtworkSprite());
            }

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(true);
            }
        }

        private void RefreshButtonState()
        {
            if (primaryButton == null)
            {
                return;
            }

            if (_progressService == null)
            {
                primaryButton.interactable = false;
                return;
            }

            var progress = _progressService.Current;
            primaryButton.interactable = _state == FailureScreenState.RetryOnly
                ? progress.CurrentLives > 0
                : progress.Gold >= keepPlayingGoldCost;

            if (closeButton != null && closeButton.gameObject.activeSelf)
            {
                closeButton.interactable = true;
            }
        }

        private string GetMessage()
        {
            return _state switch
            {
                FailureScreenState.ConfirmQuit => confirmQuitMessage,
                FailureScreenState.RetryOnly => retryMessage,
                _ => keepPlayingMessage
            };
        }

        private string GetPrimaryButtonLabel()
        {
            return _state == FailureScreenState.RetryOnly
                ? retryButtonText
                : string.Format(keepPlayingButtonFormat, keepPlayingGoldCost);
        }

        private bool ShouldShowPrimaryButtonCurrencyIcon()
        {
            return _state != FailureScreenState.RetryOnly;
        }

        private Sprite GetArtworkSprite()
        {
            return _state switch
            {
                FailureScreenState.ConfirmQuit => confirmQuitSprite,
                FailureScreenState.RetryOnly => retrySprite,
                _ => keepPlayingSprite
            };
        }

        private void ReturnToMainMenu()
        {
            if (_sceneTransitionService == null)
            {
                Debug.LogWarning("Scene transition service is not available.");
                return;
            }

            _sceneTransitionService.LoadSceneThroughLoading(mainMenuSceneName);
        }

        private void ApplyArtworkSprite(Sprite sprite)
        {
            if (artworkImage == null)
            {
                return;
            }

            if (artworkImage.sprite == sprite && _currentArtworkSprite == sprite)
            {
                artworkImage.enabled = sprite != null;
                return;
            }

            _currentArtworkSprite = sprite;
            artworkImage.sprite = sprite;
            artworkImage.enabled = sprite != null;
            PlayArtworkChangedAnimation();
        }

        private void PlayArtworkChangedAnimation()
        {
            if (artworkImage == null)
            {
                return;
            }

            KillArtworkSequence();

            var target = artworkImage.transform;
            target.localScale = _artworkBaseScale * artworkPopScale;

            _artworkSequence = DOTween.Sequence()
                .SetLink(artworkImage.gameObject, LinkBehaviour.KillOnDestroy);
            _artworkSequence.Append(target.DOScale(_artworkBaseScale * 0.92f, artworkShrinkSeconds)
                .SetEase(artworkShrinkEase));
            _artworkSequence.Append(target.DOScale(_artworkBaseScale, artworkGrowSeconds)
                .SetEase(artworkGrowEase));
        }

        private void KillArtworkSequence()
        {
            _artworkSequence?.Kill();
            _artworkSequence = null;

            if (artworkImage != null)
            {
                artworkImage.transform.localScale = _artworkBaseScale;
            }
        }

        private void SetVisible(bool visible)
        {
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
        }

        private void SubscribeToEvents()
        {
            if (_eventSubscriber == null || _failureScreenRequestedSubscription != null)
            {
                return;
            }

            _failureScreenRequestedSubscription = _eventSubscriber.Subscribe<LevelFailureScreenRequested>(_ => ShowKeepPlayingOffer());
            _keepPlayingStartedSubscription = _eventSubscriber.Subscribe<LevelKeepPlayingStarted>(_ => Hide());
            _levelRestartedSubscription = _eventSubscriber.Subscribe<LevelRestarted>(_ => Hide());
            _progressChangedSubscription = _eventSubscriber.Subscribe<MainMenuPlayerProgressChanged>(_ => Refresh());
        }

        private void UnsubscribeFromEvents()
        {
            _failureScreenRequestedSubscription?.Dispose();
            _keepPlayingStartedSubscription?.Dispose();
            _levelRestartedSubscription?.Dispose();
            _progressChangedSubscription?.Dispose();
            _failureScreenRequestedSubscription = null;
            _keepPlayingStartedSubscription = null;
            _levelRestartedSubscription = null;
            _progressChangedSubscription = null;
        }

        private void PlayButtonClickSound()
        {
            _eventPublisher?.Publish(new SoundRequested(SoundCueId.ButtonClick));
        }

        private enum FailureScreenState
        {
            KeepPlayingOffer,
            ConfirmQuit,
            RetryOnly
        }
    }
}
