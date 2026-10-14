using System;
using BlossomFlow.Application.Events;
using BlossomFlow.MainMenu.Domain;
using Reflex.Attributes;
using TMPro;
using UnityEngine;

namespace BlossomFlow.Gameplay.Presentation
{
    [DisallowMultipleComponent]
    public sealed class GoldView : MonoBehaviour
    {
        private const string DefaultGoldFormat = "{0}";

        [SerializeField] private TMP_Text goldText;
        [SerializeField] private string goldFormat = DefaultGoldFormat;

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

        private void Awake()
        {
            EnsureText();
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
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(goldFormat))
            {
                goldFormat = DefaultGoldFormat;
            }
        }

        private void Reset()
        {
            goldText = GetComponent<TMP_Text>();
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
            Refresh(progressChanged.Progress.Gold);
        }

        private void Refresh()
        {
            if (_progressService == null)
            {
                return;
            }

            Refresh(_progressService.Current.Gold);
        }

        private void Refresh(int gold)
        {
            EnsureText();
            if (goldText == null)
            {
                return;
            }

            goldText.text = string.Format(GetGoldFormat(), Mathf.Max(0, gold));
        }

        private void EnsureText()
        {
            if (goldText == null)
            {
                goldText = GetComponent<TMP_Text>();
            }
        }

        private string GetGoldFormat()
        {
            return string.IsNullOrEmpty(goldFormat) ? DefaultGoldFormat : goldFormat;
        }
    }
}
