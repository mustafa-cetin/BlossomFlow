using System;
using BlossomFlow.Application.Events;
using BlossomFlow.MainMenu.Domain;
using Reflex.Attributes;
using TMPro;
using UnityEngine;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MainMenuHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text magicalSeedText;
        [SerializeField] private TMP_Text countdownText;

        private const float CountdownRefreshSeconds = 1f;
        private const string MaxLivesCountdownText = "MAX";

        private IMainMenuPlayerProgressService _progressService;
        private IEventSubscriber _eventSubscriber;
        private IDisposable _progressChangedSubscription;
        private float _countdownRefreshTimer;

        [Inject]
        private void Construct(
            IMainMenuPlayerProgressService progressService,
            IEventSubscriber eventSubscriber)
        {
            _progressService = progressService;
            _eventSubscriber = eventSubscriber;

            if (isActiveAndEnabled)
            {
                Subscribe();
                Refresh();
            }
        }

        private void OnEnable()
        {
            _countdownRefreshTimer = 0f;
            Subscribe();
            Refresh();
        }

        private void Update()
        {
            if (_progressService == null)
            {
                return;
            }

            _countdownRefreshTimer += Time.unscaledDeltaTime;
            if (_countdownRefreshTimer < CountdownRefreshSeconds)
            {
                return;
            }

            _countdownRefreshTimer = 0f;
            Refresh();
        }

        private void OnDisable()
        {
            _progressChangedSubscription?.Dispose();
            _progressChangedSubscription = null;
        }

        private void Subscribe()
        {
            if (_eventSubscriber == null || _progressChangedSubscription != null)
            {
                return;
            }

            _progressChangedSubscription = _eventSubscriber.Subscribe<MainMenuPlayerProgressChanged>(_ => Refresh());
        }

        private void Refresh()
        {
            if (_progressService == null)
            {
                return;
            }

            var progress = _progressService.Current;
            if (livesText != null)
            {
                livesText.text = $"{progress.CurrentLives}";
            }

            if (goldText != null)
            {
                goldText.text = progress.Gold.ToString();
            }

            if (magicalSeedText != null)
            {
                magicalSeedText.text = progress.MagicalSeed.ToString();
            }

            if (countdownText != null)
            {
                var hasMaxLives = progress.CurrentLives >= progress.MaxLives;
                countdownText.text = hasMaxLives ? MaxLivesCountdownText : progress.NextLifeCountdown;
                countdownText.gameObject.SetActive(hasMaxLives || progress.HasLifeCountdown);
            }
        }
    }
}
