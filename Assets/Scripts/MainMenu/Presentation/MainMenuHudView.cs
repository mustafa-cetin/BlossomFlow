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
        [SerializeField] private TMP_Text countdownText;

        private IMainMenuPlayerProgressService _progressService;
        private IEventSubscriber _eventSubscriber;
        private IDisposable _progressChangedSubscription;

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
            Subscribe();
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
                livesText.text = $"{progress.CurrentLives}/{progress.MaxLives}";
            }

            if (goldText != null)
            {
                goldText.text = progress.Gold.ToString();
            }

            if (countdownText != null)
            {
                countdownText.text = progress.NextLifeCountdown;
                countdownText.gameObject.SetActive(progress.HasLifeCountdown);
            }
        }
    }
}
