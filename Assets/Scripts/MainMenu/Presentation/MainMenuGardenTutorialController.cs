using System;
using System.Collections;
using BlossomFlow.Application.Events;
using BlossomFlow.Application.Tutorial;
using BlossomFlow.MainMenu.Domain;
using Reflex.Attributes;
using UnityEngine;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MainMenuGardenTutorialView))]
    public sealed class MainMenuGardenTutorialController : MonoBehaviour
    {
        [SerializeField] private MainMenuGardenView gardenView;
        [SerializeField] private string plantPrompt = "Magical seed'i ek";
        [SerializeField] private string waterPrompt = "Simdi sula";
        [SerializeField] private string finalPrompt = "Play while you wait for the flower to grow";
        [SerializeField] private Vector3 plantTargetOffset = new(0f, 0.35f, 0f);
        [SerializeField] private Vector3 waterTargetOffset = new(0f, 0.25f, 0f);

        private IGardenTutorialProgressService _tutorialProgress;
        private IMainMenuPlayerProgressService _playerProgress;
        private IGardenService _gardenService;
        private IMainMenuNavigator _navigator;
        private IEventSubscriber _eventSubscriber;
        private IDisposable _progressChangedSubscription;
        private IDisposable _gardenChangedSubscription;
        private IDisposable _tabChangedSubscription;
        private IDisposable _playRequestedSubscription;
        private MainMenuGardenTutorialView _view;
        private TutorialStep _step;
        private int _targetRegionIndex = -1;

        [Inject]
        private void Construct(
            IGardenTutorialProgressService tutorialProgress,
            IMainMenuPlayerProgressService playerProgress,
            IGardenService gardenService,
            IMainMenuNavigator navigator,
            IEventSubscriber eventSubscriber)
        {
            _tutorialProgress = tutorialProgress;
            _playerProgress = playerProgress;
            _gardenService = gardenService;
            _navigator = navigator;
            _eventSubscriber = eventSubscriber;

            if (isActiveAndEnabled)
            {
                Subscribe();
                TryStartTutorial();
            }
        }

        private void Awake()
        {
            _view = GetComponent<MainMenuGardenTutorialView>();
            if (gardenView == null)
            {
                gardenView = GetComponent<MainMenuGardenView>();
            }
        }

        private void OnEnable()
        {
            Subscribe();
            if (gardenView != null)
            {
                gardenView.PlantSucceeded += OnPlantSucceeded;
                gardenView.WaterSucceeded += OnWaterSucceeded;
            }

            TryStartTutorial();
        }

        private void OnDisable()
        {
            _progressChangedSubscription?.Dispose();
            _progressChangedSubscription = null;
            _gardenChangedSubscription?.Dispose();
            _gardenChangedSubscription = null;
            _tabChangedSubscription?.Dispose();
            _tabChangedSubscription = null;
            _playRequestedSubscription?.Dispose();
            _playRequestedSubscription = null;

            if (gardenView != null)
            {
                gardenView.PlantSucceeded -= OnPlantSucceeded;
                gardenView.WaterSucceeded -= OnWaterSucceeded;
                gardenView.ClearPlantInteractionFilter();
            }

            Hide();
        }

        private void Subscribe()
        {
            if (_eventSubscriber == null)
            {
                return;
            }

            _progressChangedSubscription ??= _eventSubscriber.Subscribe<MainMenuPlayerProgressChanged>(_ => TryStartTutorial());
            _gardenChangedSubscription ??= _eventSubscriber.Subscribe<MainMenuGardenChanged>(_ => RefreshCurrentPrompt());
            _tabChangedSubscription ??= _eventSubscriber.Subscribe<MainMenuTabChanged>(OnTabChanged);
            _playRequestedSubscription ??= _eventSubscriber.Subscribe<MainMenuPlayRequested>(_ => HideFinalMessage());
        }

        private void TryStartTutorial()
        {
            if (_step != TutorialStep.Inactive ||
                _tutorialProgress == null ||
                _tutorialProgress.IsCompleted ||
                _playerProgress == null ||
                _gardenService == null ||
                gardenView == null)
            {
                return;
            }

            var progress = _playerProgress.Current;
            if (progress.LevelNumber < _gardenService.UnlockLevel || !_gardenService.IsUnlocked)
            {
                return;
            }

            if (TryStartAtWaterStep())
            {
                return;
            }

            if (progress.MagicalSeed <= 0)
            {
                return;
            }

            _targetRegionIndex = FindFirstEmptyRegion();
            if (_targetRegionIndex < 0)
            {
                CompleteTutorial();
                return;
            }

            _step = TutorialStep.AwaitingPlant;
            _navigator?.TrySelect(MainMenuTab.Garden);
            gardenView.SetPlantInteractionFilter(regionIndex => regionIndex == _targetRegionIndex);
            StartCoroutine(PointAtPlantNextFrame());
        }

        private bool TryStartAtWaterStep()
        {
            var garden = _gardenService.Current;
            if (!garden.HasGrowingPlots(DateTime.UtcNow.Ticks))
            {
                return false;
            }

            _step = TutorialStep.AwaitingWater;
            _navigator?.TrySelect(MainMenuTab.Garden);
            gardenView.ClearPlantInteractionFilter();
            StartCoroutine(PointAtWaterNextFrame());
            return true;
        }

        private IEnumerator PointAtPlantNextFrame()
        {
            yield return null;
            RefreshCurrentPrompt();
        }

        private void RefreshCurrentPrompt()
        {
            if (_step == TutorialStep.AwaitingPlant)
            {
                if (gardenView != null && gardenView.TryGetPlotAnchor(_targetRegionIndex, out var position))
                {
                    _view.PointAt(position + plantTargetOffset, plantPrompt);
                }

                return;
            }

            if (_step == TutorialStep.AwaitingWater)
            {
                if (gardenView != null && gardenView.TryGetWaterButtonAnchor(out var position))
                {
                    _view.PointAt(position + waterTargetOffset, waterPrompt);
                }
            }
        }

        private int FindFirstEmptyRegion()
        {
            var garden = _gardenService.Current;
            for (var i = 0; i < 64; i++)
            {
                if (!garden.IsPlanted(i) && gardenView.TryGetPlotAnchor(i, out _))
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnPlantSucceeded(int regionIndex)
        {
            if (_step != TutorialStep.AwaitingPlant || regionIndex != _targetRegionIndex)
            {
                return;
            }

            gardenView.ClearPlantInteractionFilter();
            _step = TutorialStep.AwaitingWater;
            StartCoroutine(PointAtWaterNextFrame());
        }

        private IEnumerator PointAtWaterNextFrame()
        {
            yield return null;
            RefreshCurrentPrompt();
        }

        private void OnWaterSucceeded()
        {
            if (_step != TutorialStep.AwaitingWater)
            {
                return;
            }

            ShowFinalMessageAndComplete();
        }

        private void ShowFinalMessageAndComplete()
        {
            _tutorialProgress?.MarkCompleted();
            gardenView?.ClearPlantInteractionFilter();
            _step = TutorialStep.ShowingFinalMessage;
            _view?.ShowCenterMessage(finalPrompt);
        }

        private void CompleteTutorial()
        {
            _tutorialProgress?.MarkCompleted();
            gardenView?.ClearPlantInteractionFilter();
            _step = TutorialStep.Completed;
            HideVisual();
        }

        private void OnTabChanged(MainMenuTabChanged tabChanged)
        {
            if (tabChanged.CurrentTab == MainMenuTab.Home)
            {
                HideFinalMessage();
            }
        }

        private void HideFinalMessage()
        {
            if (_step != TutorialStep.ShowingFinalMessage && _step != TutorialStep.Completed)
            {
                return;
            }

            _step = TutorialStep.Completed;
            HideVisual();
        }

        private void Hide()
        {
            _step = TutorialStep.Inactive;
            HideVisual();
        }

        private void HideVisual()
        {
            _view?.Hide();
        }

        private enum TutorialStep
        {
            Inactive,
            AwaitingPlant,
            AwaitingWater,
            ShowingFinalMessage,
            Completed
        }
    }
}
