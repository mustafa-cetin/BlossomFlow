using System;
using BlossomFlow.Application.Audio;
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
    public sealed class MainMenuGardenView : MonoBehaviour
    {
        [SerializeField] private MainMenuGardenPlotView[] plotViews;
        [SerializeField, Min(1)] private int regionCount = 19;
        [SerializeField] private Transform plotRoot;
        [SerializeField] private MainMenuGardenPlotView plotPrefab;
        [SerializeField] private GameObject lockedUntilLevelObject;
        [SerializeField] private Button waterButton;
        [SerializeField] private TMP_Text waterCountdownText;
        [SerializeField] private TMP_Text magicalSeedText;
        [SerializeField] private string magicalSeedFormat = "Magical Seed: {0}";
        [SerializeField, Min(0f)] private float growthHours = 24f;
        [SerializeField, Min(0f)] private float waterReductionHours = 1f;
        [SerializeField, Min(0f)] private float waterCooldownMinutes = 10f;
        [SerializeField, Min(1f)] private float waterButtonPulseScale = 1.08f;
        [SerializeField, Min(0.01f)] private float waterButtonPulseSeconds = 0.45f;
        [SerializeField] private Ease waterButtonPulseEase = Ease.InOutSine;
        [SerializeField] private bool collectPlotViewsFromChildren = true;

        private const float RefreshIntervalSeconds = 1f;

        private IGardenService _gardenService;
        private IMainMenuPlayerProgressService _progressService;
        private IEventPublisher _eventPublisher;
        private IEventSubscriber _eventSubscriber;
        private IDisposable _gardenChangedSubscription;
        private IDisposable _progressChangedSubscription;
        private float _refreshTimer;
        private Tween _waterButtonPulseTween;
        private Vector3 _waterButtonInitialScale;
        private bool _hasWaterButtonInitialScale;
        private Func<int, bool> _plantInteractionFilter;

        public event Action<int> PlantSucceeded;
        public event Action WaterSucceeded;

        [Inject]
        private void Construct(
            IGardenService gardenService,
            IMainMenuPlayerProgressService progressService,
            IEventPublisher eventPublisher,
            IEventSubscriber eventSubscriber)
        {
            _gardenService = gardenService;
            _progressService = progressService;
            _eventPublisher = eventPublisher;
            _eventSubscriber = eventSubscriber;

            if (isActiveAndEnabled)
            {
                Subscribe();
                InitializePlots();
                Refresh();
            }
        }

        private void Awake()
        {
            InitializePlots();
        }

        private void OnEnable()
        {
            if (waterButton != null)
            {
                waterButton.onClick.AddListener(Water);
            }

            _refreshTimer = 0f;
            Subscribe();
            InitializePlots();
            Refresh();
        }

        private void Update()
        {
            if (_gardenService == null)
            {
                return;
            }

            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer < RefreshIntervalSeconds)
            {
                return;
            }

            _refreshTimer = 0f;
            Refresh();
        }

        private void OnDisable()
        {
            StopWaterButtonPulse();

            if (waterButton != null)
            {
                waterButton.onClick.RemoveListener(Water);
            }

            _gardenChangedSubscription?.Dispose();
            _gardenChangedSubscription = null;
            _progressChangedSubscription?.Dispose();
            _progressChangedSubscription = null;
        }

        private void Subscribe()
        {
            if (_eventSubscriber == null)
            {
                return;
            }

            _gardenChangedSubscription ??= _eventSubscriber.Subscribe<MainMenuGardenChanged>(_ => Refresh());
            _progressChangedSubscription ??= _eventSubscriber.Subscribe<MainMenuPlayerProgressChanged>(_ => Refresh());
        }

        private void InitializePlots()
        {
            regionCount = Mathf.Max(1, regionCount);
            if (plotRoot == null)
            {
                plotRoot = transform;
            }

            if ((plotViews == null || plotViews.Length == 0) && collectPlotViewsFromChildren)
            {
                plotViews = GetComponentsInChildren<MainMenuGardenPlotView>(true);
            }

            CreateMissingPlots();

            if (plotViews == null)
            {
                return;
            }

            for (var i = 0; i < plotViews.Length; i++)
            {
                if (plotViews[i] == null)
                {
                    continue;
                }

                plotViews[i].Initialize(i, Plant);
                plotViews[i].gameObject.SetActive(i < regionCount);
            }
        }

        private void Refresh()
        {
            if (_gardenService == null || _progressService == null)
            {
                return;
            }

            var garden = _gardenService.Current;
            var isUnlocked = _gardenService.IsUnlocked;
            var canPlant = _gardenService.CanPlant;
            var progress = _progressService.Current;
            var nowTicks = DateTime.UtcNow.Ticks;

            if (lockedUntilLevelObject != null)
            {
                lockedUntilLevelObject.SetActive(!isUnlocked);
            }

            RefreshWaterButton(isUnlocked);

            if (magicalSeedText != null)
            {
                magicalSeedText.text = string.Format(magicalSeedFormat, progress.MagicalSeed);
            }

            if (plotViews == null)
            {
                return;
            }

            var refreshCount = Mathf.Min(regionCount, plotViews.Length);
            for (var i = 0; i < refreshCount; i++)
            {
                if (plotViews[i] == null)
                {
                    continue;
                }

                var isPlanted = garden.IsPlanted(i);
                var isGrown = garden.IsGrown(i, nowTicks);
                var countdown = isPlanted && !isGrown
                    ? FormatCountdown(garden.GetRemainingGrowthTime(i, nowTicks))
                    : string.Empty;
                var canPlantHere = isUnlocked && canPlant && (_plantInteractionFilter == null || _plantInteractionFilter(i));
                plotViews[i].SetState(isPlanted, isGrown, countdown, canPlantHere);
            }
        }

        private void CreateMissingPlots()
        {
            var existingCount = plotViews?.Length ?? 0;
            if (existingCount >= regionCount || plotPrefab == null || plotRoot == null)
            {
                return;
            }

            var nextPlotViews = new MainMenuGardenPlotView[regionCount];
            for (var i = 0; i < existingCount; i++)
            {
                nextPlotViews[i] = plotViews[i];
            }

            for (var i = existingCount; i < regionCount; i++)
            {
                nextPlotViews[i] = Instantiate(plotPrefab, plotRoot);
            }

            plotViews = nextPlotViews;
        }

        private void Plant(int regionIndex)
        {
            if (_plantInteractionFilter != null && !_plantInteractionFilter(regionIndex))
            {
                return;
            }

            if (_gardenService != null && _gardenService.TryPlant(regionIndex, GetTiming()))
            {
                _eventPublisher?.Publish(new SoundRequested(SoundCueId.ButtonClick));
                PlantSucceeded?.Invoke(regionIndex);
                Refresh();
            }
        }

        private void Water()
        {
            if (_gardenService != null && _gardenService.TryWater(GetTiming()))
            {
                _eventPublisher?.Publish(new SoundRequested(SoundCueId.ButtonClick));
                WaterSucceeded?.Invoke();
                Refresh();
            }
        }

        public void SetPlantInteractionFilter(Func<int, bool> filter)
        {
            _plantInteractionFilter = filter;
            Refresh();
        }

        public void ClearPlantInteractionFilter()
        {
            _plantInteractionFilter = null;
            Refresh();
        }

        public bool TryGetPlotAnchor(int regionIndex, out Vector3 position)
        {
            position = Vector3.zero;
            if (plotViews == null || regionIndex < 0 || regionIndex >= plotViews.Length || plotViews[regionIndex] == null)
            {
                return false;
            }

            position = plotViews[regionIndex].transform.position;
            return true;
        }

        public bool TryGetWaterButtonAnchor(out Vector3 position)
        {
            position = Vector3.zero;
            if (waterButton == null)
            {
                return false;
            }

            position = waterButton.transform.position;
            return true;
        }

        private void RefreshWaterButton(bool isUnlocked)
        {
            var canWater = isUnlocked && _gardenService.CanWater;
            if (waterButton != null)
            {
                waterButton.gameObject.SetActive(isUnlocked);
                waterButton.interactable = canWater;
            }

            if (canWater)
            {
                StartWaterButtonPulse();
            }
            else
            {
                StopWaterButtonPulse();
            }

            if (waterCountdownText != null)
            {
                var countdown = _gardenService.WaterCountdown;
                waterCountdownText.text = countdown;
                waterCountdownText.gameObject.SetActive(isUnlocked && !canWater && !string.IsNullOrWhiteSpace(countdown));
            }
        }

        private MainMenuGardenTiming GetTiming()
        {
            return new MainMenuGardenTiming(
                TimeSpan.FromHours(growthHours),
                TimeSpan.FromHours(waterReductionHours),
                TimeSpan.FromMinutes(waterCooldownMinutes));
        }

        private void StartWaterButtonPulse()
        {
            if (waterButton == null || _waterButtonPulseTween != null)
            {
                return;
            }

            var target = waterButton.transform;
            if (!_hasWaterButtonInitialScale)
            {
                _waterButtonInitialScale = target.localScale;
                _hasWaterButtonInitialScale = true;
            }

            target.localScale = _waterButtonInitialScale;
            _waterButtonPulseTween = target
                .DOScale(_waterButtonInitialScale * waterButtonPulseScale, waterButtonPulseSeconds)
                .SetEase(waterButtonPulseEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(waterButton.gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void StopWaterButtonPulse()
        {
            _waterButtonPulseTween?.Kill();
            _waterButtonPulseTween = null;

            if (waterButton != null && _hasWaterButtonInitialScale)
            {
                waterButton.transform.localScale = _waterButtonInitialScale;
            }
        }

        private static string FormatCountdown(TimeSpan remaining)
        {
            return remaining.TotalHours >= 1d
                ? $"{(int)remaining.TotalHours}:{remaining.Minutes:00}:{remaining.Seconds:00}"
                : $"{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
    }
}
