using System;
using BlossomFlow.Application.Events;
using BlossomFlow.Application.Levels;
using BlossomFlow.Gameplay.Application.Levels;
using BlossomFlow.MainMenu.Domain;
using Reflex.Attributes;
using TMPro;
using UnityEngine;

namespace BlossomFlow.Gameplay.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CurrentLevelNumberView : MonoBehaviour
    {
        private const string DefaultLevelFormat = "Level {0}";

        [SerializeField] private TMP_Text levelText;
        [SerializeField] private string levelFormat = DefaultLevelFormat;

        private IMainMenuPlayerProgressService _progressService;
        private ICurrentLevelDefinitionProvider _currentLevelProvider;
        private IEventSubscriber _eventSubscriber;
        private IDisposable _progressChangedSubscription;
        private IDisposable _levelAdvancedSubscription;

        [Inject]
        private void Construct(
            IMainMenuPlayerProgressService progressService,
            IEventSubscriber eventSubscriber,
            ICurrentLevelDefinitionProvider currentLevelProvider)
        {
            _progressService = progressService;
            _eventSubscriber = eventSubscriber;
            _currentLevelProvider = currentLevelProvider;

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
            if (string.IsNullOrEmpty(levelFormat))
            {
                levelFormat = DefaultLevelFormat;
            }
        }

        private void Reset()
        {
            levelText = GetComponent<TMP_Text>();
        }

        private void Subscribe()
        {
            if (_eventSubscriber == null || _progressChangedSubscription != null)
            {
                return;
            }

            _progressChangedSubscription = _eventSubscriber.Subscribe<MainMenuPlayerProgressChanged>(_ => Refresh());
            _levelAdvancedSubscription = _eventSubscriber.Subscribe<LevelAdvanced>(_ => Refresh());
        }

        private void Unsubscribe()
        {
            _progressChangedSubscription?.Dispose();
            _progressChangedSubscription = null;
            _levelAdvancedSubscription?.Dispose();
            _levelAdvancedSubscription = null;
        }

        private void Refresh()
        {
            if (_progressService == null && _currentLevelProvider == null)
            {
                return;
            }

            EnsureText();
            if (levelText == null)
            {
                return;
            }

            levelText.text = string.Format(GetLevelFormat(), GetCurrentLevelNumber());
        }

        private int GetCurrentLevelNumber()
        {
            if (_progressService != null)
            {
                return _progressService.Current.LevelNumber;
            }

            return _currentLevelProvider != null ? _currentLevelProvider.CurrentLevelNumber : 1;
        }

        private void EnsureText()
        {
            if (levelText == null)
            {
                levelText = GetComponent<TMP_Text>();
            }
        }

        private string GetLevelFormat()
        {
            return string.IsNullOrEmpty(levelFormat) ? DefaultLevelFormat : levelFormat;
        }
    }
}
