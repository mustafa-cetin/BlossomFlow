using System;
using BlossomFlow.Application.Events;
using BlossomFlow.MainMenu.Domain;
using DG.Tweening;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class MainMenuPlayButtonView : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private string labelFormat = "Level {0}";
        [SerializeField] private float breathingScale = 1.05f;
        [SerializeField] private float breathingSeconds = 0.9f;
        [SerializeField] private Ease breathingEase = Ease.InOutSine;

        private Button _button;
        private IEventPublisher _eventPublisher;
        private IEventSubscriber _eventSubscriber;
        private IMainMenuPlayerProgressService _progressService;
        private IDisposable _progressChangedSubscription;
        private Tween _breathingTween;
        private Vector3 _initialScale;

        [Inject]
        private void Construct(
            IEventPublisher eventPublisher,
            IEventSubscriber eventSubscriber,
            IMainMenuPlayerProgressService progressService)
        {
            _eventPublisher = eventPublisher;
            _eventSubscriber = eventSubscriber;
            _progressService = progressService;

            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            RefreshLabel();
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            _initialScale = transform.localScale;
            RefreshLabel();
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(RequestPlay);
            }

            StartBreathing();
            Subscribe();
            RefreshLabel();
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(RequestPlay);
            }

            StopBreathing();
            _progressChangedSubscription?.Dispose();
            _progressChangedSubscription = null;
        }

        private void RequestPlay()
        {
            if (_eventPublisher == null || _progressService == null)
            {
                return;
            }

            var progress = _progressService.Current;
            if (progress.CurrentLives <= 0)
            {
                RefreshLabel();
                return;
            }

            _eventPublisher.Publish(new MainMenuPlayRequested(progress.LevelNumber));
        }

        private void Subscribe()
        {
            if (_eventSubscriber == null || _progressChangedSubscription != null)
            {
                return;
            }

            _progressChangedSubscription = _eventSubscriber.Subscribe<MainMenuPlayerProgressChanged>(_ => RefreshLabel());
        }

        private void RefreshLabel()
        {
            if (_progressService == null)
            {
                return;
            }

            var progress = _progressService.Current;
            if (labelText != null)
            {
                labelText.text = string.Format(labelFormat, progress.LevelNumber);
            }

            if (_button != null)
            {
                _button.interactable = progress.CurrentLives > 0;
            }
        }

        private void StartBreathing()
        {
            StopBreathing();
            transform.localScale = _initialScale;
            _breathingTween = transform.DOScale(_initialScale * breathingScale, breathingSeconds)
                .SetEase(breathingEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void StopBreathing()
        {
            _breathingTween?.Kill();
            _breathingTween = null;
            transform.localScale = _initialScale;
        }
    }
}
