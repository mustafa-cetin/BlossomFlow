using System;
using System.Collections;
using BlossomFlow.Application.Events;
using BlossomFlow.MainMenu.Domain;
using Reflex.Attributes;
using TMPro;
using UnityEngine;

namespace BlossomFlow.Gameplay.Presentation
{
    [DisallowMultipleComponent]
    public sealed class LiveView : MonoBehaviour
    {
        private const string DefaultLivesFormat = "{0}/{1}";
        private const string DefaultCountdownFormat = "{0}";
        private const string MaxLivesCountdownText = "MAX";

        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private string livesFormat = DefaultLivesFormat;
        [SerializeField] private string countdownFormat = DefaultCountdownFormat;

        private IMainMenuPlayerProgressService _progressService;
        private IEventSubscriber _eventSubscriber;
        private IDisposable _progressChangedSubscription;
        private Coroutine _countdownRefreshRoutine;

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

        private void Awake()
        {
            EnsureLivesText();
            Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopCountdownRefreshRoutine();
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(livesFormat))
            {
                livesFormat = DefaultLivesFormat;
            }

            if (string.IsNullOrEmpty(countdownFormat))
            {
                countdownFormat = DefaultCountdownFormat;
            }
        }

        private void Reset()
        {
            livesText = GetComponent<TMP_Text>();
        }

        private void Subscribe()
        {
            if (_eventSubscriber == null || _progressChangedSubscription != null)
            {
                return;
            }

            _progressChangedSubscription = _eventSubscriber.Subscribe<MainMenuPlayerProgressChanged>(OnProgressChanged);
        }

        private void Unsubscribe()
        {
            _progressChangedSubscription?.Dispose();
            _progressChangedSubscription = null;
        }

        private void OnProgressChanged(MainMenuPlayerProgressChanged progressChanged)
        {
            Refresh(progressChanged.Progress);
        }

        private void Refresh()
        {
            if (_progressService == null)
            {
                return;
            }

            Refresh(_progressService.Current);
        }

        private void Refresh(MainMenuPlayerProgress progress)
        {
            EnsureLivesText();
            if (livesText != null)
            {
                livesText.text = string.Format(
                    GetLivesFormat(),
                    Mathf.Max(0, progress.CurrentLives),
                    Mathf.Max(1, progress.MaxLives));
            }

            RefreshCountdown(progress);
            SyncCountdownRefreshRoutine(progress);
        }

        private void RefreshCountdown(MainMenuPlayerProgress progress)
        {
            if (countdownText == null)
            {
                return;
            }

            var hasMaxLives = progress.CurrentLives >= progress.MaxLives;
            var hasCountdown = progress.HasLifeCountdown;
            countdownText.gameObject.SetActive(hasMaxLives || hasCountdown);
            countdownText.text = hasMaxLives
                ? MaxLivesCountdownText
                : string.Format(GetCountdownFormat(), progress.NextLifeCountdown);
        }

        private void SyncCountdownRefreshRoutine(MainMenuPlayerProgress progress)
        {
            if (progress.HasLifeCountdown)
            {
                StartCountdownRefreshRoutine();
                return;
            }

            StopCountdownRefreshRoutine();
        }

        private void StartCountdownRefreshRoutine()
        {
            if (_countdownRefreshRoutine != null || !isActiveAndEnabled)
            {
                return;
            }

            _countdownRefreshRoutine = StartCoroutine(CountdownRefreshRoutine());
        }

        private void StopCountdownRefreshRoutine()
        {
            if (_countdownRefreshRoutine == null)
            {
                return;
            }

            StopCoroutine(_countdownRefreshRoutine);
            _countdownRefreshRoutine = null;
        }

        private IEnumerator CountdownRefreshRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                Refresh();
            }
        }

        private void EnsureLivesText()
        {
            if (livesText == null)
            {
                livesText = GetComponent<TMP_Text>();
            }
        }

        private string GetLivesFormat()
        {
            return string.IsNullOrEmpty(livesFormat) ? DefaultLivesFormat : livesFormat;
        }

        private string GetCountdownFormat()
        {
            return string.IsNullOrEmpty(countdownFormat) ? DefaultCountdownFormat : countdownFormat;
        }
    }
}
