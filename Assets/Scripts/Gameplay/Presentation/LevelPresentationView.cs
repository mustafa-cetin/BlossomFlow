using System;
using System.Collections.Generic;
using BlossomFlow.Gameplay.Domain;
using BlossomFlow.Gameplay.Domain.Levels;
using DG.Tweening;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BlossomFlow.Gameplay.Presentation
{
    public sealed class LevelPresentationView : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private Transform flowerRoot;
        [SerializeField] private Transform conveyorRoot;
        [SerializeField] private Transform launchColumnRoot;
        [SerializeField] private Transform waitingSlotRoot;

        [Header("Optional Prefabs")]
        [SerializeField] private GameObject flowerPrefab;
        [SerializeField] private GameObject potPrefab;
        [SerializeField] private GameObject waitingSlotPrefab;

        [Header("Flower Sprites")]
        [SerializeField] private FlowerSpriteBinding[] flowerSprites =
        {
            new FlowerSpriteBinding(FlowerColor.Red),
            new FlowerSpriteBinding(FlowerColor.Yellow),
            new FlowerSpriteBinding(FlowerColor.Blue),
            new FlowerSpriteBinding(FlowerColor.Pink),
            new FlowerSpriteBinding(FlowerColor.Purple),
            new FlowerSpriteBinding(FlowerColor.White),
            new FlowerSpriteBinding(FlowerColor.Orange),
            new FlowerSpriteBinding(FlowerColor.Black),
            new FlowerSpriteBinding(FlowerColor.Cyan),
            new FlowerSpriteBinding(FlowerColor.Magenta)
        };

        [Header("Layout")]
        [SerializeField] private Vector2 flowerOrigin = new Vector2(-2f, 0f);
        [SerializeField] private Vector2 launchOrigin = new Vector2(-3.2f, -3.5f);
        [SerializeField] private Vector2 waitingOrigin = new Vector2(-3.2f, -2.35f);
        [SerializeField] private Vector2 temporaryWaitingSlotOffset = new Vector2(0.9f, 0f);
        [SerializeField, Range(0f, 1f)] private float temporaryWaitingSlotAlpha = 0.35f;
        [SerializeField] private float cellSize = 0.8f;
        [SerializeField] private int referenceGridAxis = 9;
        [SerializeField] private float flowerScaleMultiplier = 0.82f;
        [SerializeField] private float potVisualSize = 0.34f;
        [SerializeField] private float waitingSlotVisualSize = 0.42f;
        [SerializeField] private float potSpacing = 0.75f;
        [SerializeField] private float queuedPotVerticalSpacing = 0.46f;
        [SerializeField] private float minimumVisualGap = 0.06f;
        [SerializeField] private float conveyorPathGapInCells = 1f;
        [SerializeField] private float conveyorStepSeconds = 0.45f;
        [SerializeField] private bool autoFitCamera = true;
        [SerializeField] private float cameraPadding = 0.6f;

        [Header("Animation")]
        [SerializeField] private float conveyorMoveSeconds = 0.45f;
        [SerializeField] private float launchMoveSeconds = 0.38f;
        [SerializeField] private float waitingMoveSeconds = 0.35f;
        [SerializeField] private float shuffleMoveSeconds = 0.32f;
        [SerializeField] private float flowerFlySeconds = 0.22f;
        [SerializeField] private float potDisappearSeconds = 0.24f;
        [SerializeField] private float flowerFlyArcHeight = 0.35f;
        [SerializeField] private float waitingPotPulseScale = 1.08f;
        [SerializeField] private float waitingPotPulseSeconds = 0.55f;
        [SerializeField] private Ease launchMoveEase = Ease.OutCubic;
        [SerializeField] private Ease waitingMoveEase = Ease.OutCubic;
        [SerializeField] private Ease shuffleMoveEase = Ease.InOutCubic;
        [SerializeField] private Ease waitingPotPulseEase = Ease.InOutSine;
        [SerializeField] private Ease flowerFlyEase = Ease.InCubic;
        [SerializeField] private Ease potDisappearEase = Ease.InBack;

        [Header("Editor Preview")]
        [SerializeField] private bool showLayoutGizmos = true;
        [SerializeField] private bool showVisualFootprintGizmos = true;
        [SerializeField] private int previewTemporaryWaitingSlotCount = 3;

        private readonly Dictionary<FlowerCell, GameObject> _flowerViews = new();
        private readonly Dictionary<PotModel, GameObject> _potViews = new();
        private readonly Dictionary<int, GameObject> _waitingPotViews = new();
        private readonly Dictionary<GameObject, Tween> _waitingPotPulseTweens = new();
        private readonly Dictionary<int, List<GameObject>> _launchColumnViews = new();
        private readonly Dictionary<FlowerCell, PotModel> _pendingFlowerTargets = new();
        private readonly Dictionary<PotModel, int> _collectedFlowerCounts = new();
        private readonly HashSet<PotModel> _trackedPotCounters = new();
        private readonly Dictionary<int, GameObject> _waitingSlotViews = new();
        private readonly Dictionary<int, GameObject> _linkedPotViews = new();
        private readonly Dictionary<PotModel, GameObject> _queuedPotViews = new();

        private LevelSession _session;
        private Sprite _fallbackSprite;
        private Camera _camera;
        private float _runtimeCellSize;
        private float _runtimeFlowerScale;
        private float _runtimePotSpacing;
        private float _runtimeQueuedPotVerticalSpacing;

        private const float MinimumWorldSize = 0.01f;
        private const float BackQueuedPotScale = 0.82f;

        public event Action<int> LaunchColumnRequested;
        public event Action<int> WaitingPotRecallRequested;

        public int ReferenceGridAxis => referenceGridAxis;
        public float GetGridScaledDuration(float duration) => ScaleDurationForGrid(duration);

        public bool TryGetLaunchColumnAnchor(int columnIndex, out Vector3 position)
        {
            if (_launchColumnViews.TryGetValue(columnIndex, out var views) && views != null)
            {
                for (var i = 0; i < views.Count; i++)
                {
                    if (views[i] != null)
                    {
                        position = views[i].transform.position;
                        return true;
                    }
                }
            }

            if (_session != null && columnIndex >= 0 && columnIndex < _session.LaunchColumns.Count)
            {
                position = LaunchPosition(columnIndex, 0);
                return true;
            }

            position = default;
            return false;
        }

        public bool TryGetWaitingPotAnchor(int slotIndex, out Vector3 position)
        {
            if (_waitingSlotViews.TryGetValue(slotIndex, out var waitingSlotView) && waitingSlotView != null)
            {
                position = waitingSlotView.transform.position;
                return true;
            }

            if (_waitingPotViews.TryGetValue(slotIndex, out var waitingPotView) && waitingPotView != null)
            {
                position = waitingPotView.transform.position;
                return true;
            }

            if (_session != null && slotIndex >= 0)
            {
                position = WaitingPotPosition(slotIndex);
                return true;
            }

            position = default;
            return false;
        }

        public void BindSession(LevelSession session)
        {
            if (session == null)
            {
                return;
            }

            UnbindSession();
            _session = session;
            EnsureRoots();
            CalculateRuntimeLayout();
            _camera = Camera.main;
            FitCameraToLevel();
            _session.FlowerGrid.FlowerRemoved += OnFlowerRemoved;
            _session.WaitingPotPlaced += OnWaitingPotPlaced;
            _session.WaitingPotRecalled += OnWaitingPotRecalled;
            _session.TemporaryWaitingSlotCreated += OnTemporaryWaitingSlotCreated;
            _session.TemporaryWaitingSlotRemoved += OnTemporaryWaitingSlotRemoved;
            _session.Conveyor.PotRemoved += OnConveyorPotRemoved;
        }

        public void UnbindSession()
        {
            if (_session != null)
            {
                _session.FlowerGrid.FlowerRemoved -= OnFlowerRemoved;
                _session.WaitingPotPlaced -= OnWaitingPotPlaced;
                _session.WaitingPotRecalled -= OnWaitingPotRecalled;
                _session.TemporaryWaitingSlotCreated -= OnTemporaryWaitingSlotCreated;
                _session.TemporaryWaitingSlotRemoved -= OnTemporaryWaitingSlotRemoved;
                _session.Conveyor.PotRemoved -= OnConveyorPotRemoved;
            }

            foreach (var pot in _trackedPotCounters)
            {
                pot.StateChanged -= OnPotStateChanged;
            }

            _trackedPotCounters.Clear();
            _session = null;
        }

        public void ClearViews()
        {
            ClearRoot(flowerRoot);
            ClearRoot(conveyorRoot);
            ClearRoot(launchColumnRoot);
            ClearRoot(waitingSlotRoot);
            ClearLinkedPotViews();

            _flowerViews.Clear();
            _potViews.Clear();
            _waitingPotViews.Clear();
            _waitingPotPulseTweens.Clear();
            _launchColumnViews.Clear();
            _pendingFlowerTargets.Clear();
            _collectedFlowerCounts.Clear();
            _waitingSlotViews.Clear();
            _queuedPotViews.Clear();
        }

        public void ShowWinResolution()
        {
            foreach (var view in _potViews.Values)
            {
                if (view != null)
                {
                    view.transform.DOPunchScale(Vector3.one * potVisualSize * 0.18f, 0.28f, 6, 0.45f)
                        .SetLink(view, LinkBehaviour.KillOnDestroy);
                }
            }
        }

        public void ShowFailResolution(PotModel failedPot)
        {
            if (failedPot == null || !_potViews.TryGetValue(failedPot, out var view) || view == null)
            {
                return;
            }

            StopWaitingPotPulse(view);
            SetColor(view, Color.black);
            view.transform.DOKill();
            view.transform.DOPunchRotation(new Vector3(0f, 0f, 18f), 0.35f, 8, 0.55f)
                .SetLink(view, LinkBehaviour.KillOnDestroy);
        }

        public void BuildInitialViews()
        {
            if (_session == null)
            {
                return;
            }

            BuildFlowerViews();
            BuildLaunchColumnViews();
            BuildWaitingSlotViews();
        }

        public void RefreshLaunchColumnViews()
        {
            if (_session == null)
            {
                return;
            }

            BuildLaunchColumnViews();
        }

        public void PlayShuffleLaunchQueueAnimation()
        {
            if (_session == null)
            {
                return;
            }

            var previousQueuedPotViews = new Dictionary<PotModel, GameObject>(_queuedPotViews);
            var activeQueuedPotViews = new Dictionary<PotModel, GameObject>();
            var activeViews = new HashSet<GameObject>();
            _launchColumnViews.Clear();

            for (var columnIndex = 0; columnIndex < _session.LaunchColumns.Count; columnIndex++)
            {
                var column = _session.LaunchColumns[columnIndex];
                if (!column.CanLaunch)
                {
                    continue;
                }

                var views = new List<GameObject>();
                var pots = column.Pots;
                for (var queueIndex = 0; queueIndex < pots.Count; queueIndex++)
                {
                    var pot = pots[queueIndex];
                    if (!previousQueuedPotViews.TryGetValue(pot, out var view) || view == null)
                    {
                        view = CreateView(potPrefab, launchColumnRoot, $"LaunchPot_{columnIndex}_{queueIndex}");
                        view.transform.position = LaunchPosition(columnIndex, queueIndex);
                    }

                    view.name = $"LaunchPot_{columnIndex}_{queueIndex}";
                    view.transform.SetParent(launchColumnRoot, true);
                    ConfigurePotView(view, pot);
                    SetSortingOrder(view, 20 - queueIndex);

                    if (queueIndex == 0)
                    {
                        var capturedColumn = columnIndex;
                        BindClick(view, () => LaunchColumnRequested?.Invoke(capturedColumn), LevelClickTargetKind.LaunchColumn, capturedColumn);
                    }
                    else
                    {
                        DisableClick(view);
                    }

                    AnimateLaunchQueueMove(view, LaunchPosition(columnIndex, queueIndex), GetPotVisualSize(queueIndex), shuffleMoveSeconds, shuffleMoveEase);
                    views.Add(view);
                    activeViews.Add(view);
                    activeQueuedPotViews[pot] = view;
                }

                _launchColumnViews[columnIndex] = views;
            }

            foreach (var pair in previousQueuedPotViews)
            {
                if (pair.Value != null && !activeViews.Contains(pair.Value))
                {
                    DestroyLaunchColumnView(pair.Value);
                }
            }

            _queuedPotViews.Clear();
            foreach (var pair in activeQueuedPotViews)
            {
                _queuedPotViews[pair.Key] = pair.Value;
            }

            RefreshLinkedPotViews();
        }

        private void OnDestroy()
        {
            UnbindSession();
        }

        private void OnValidate()
        {
            cellSize = Mathf.Max(MinimumWorldSize, cellSize);
            temporaryWaitingSlotAlpha = Mathf.Clamp01(temporaryWaitingSlotAlpha);
            referenceGridAxis = Mathf.Max(2, referenceGridAxis);
            flowerScaleMultiplier = Mathf.Max(MinimumWorldSize, flowerScaleMultiplier);
            potVisualSize = Mathf.Max(MinimumWorldSize, potVisualSize);
            waitingSlotVisualSize = Mathf.Max(MinimumWorldSize, waitingSlotVisualSize);
            minimumVisualGap = Mathf.Max(0f, minimumVisualGap);
            potSpacing = Mathf.Max(Mathf.Max(MinimumWorldSize, potSpacing), GetMinimumPotSpacing());
            queuedPotVerticalSpacing = Mathf.Max(Mathf.Max(MinimumWorldSize, queuedPotVerticalSpacing), GetMinimumQueuedPotSpacing());
            conveyorPathGapInCells = Mathf.Max(0f, conveyorPathGapInCells);
            conveyorStepSeconds = Mathf.Max(MinimumWorldSize, conveyorStepSeconds);
            cameraPadding = Mathf.Max(0f, cameraPadding);
            conveyorMoveSeconds = Mathf.Max(conveyorStepSeconds, conveyorMoveSeconds);
            launchMoveSeconds = Mathf.Max(0f, launchMoveSeconds);
            waitingMoveSeconds = Mathf.Max(0f, waitingMoveSeconds);
            flowerFlySeconds = Mathf.Max(0f, flowerFlySeconds);
            shuffleMoveSeconds = Mathf.Max(0f, shuffleMoveSeconds);
            potDisappearSeconds = Mathf.Max(0f, potDisappearSeconds);
            flowerFlyArcHeight = Mathf.Max(0f, flowerFlyArcHeight);
            waitingPotPulseScale = Mathf.Max(1f, waitingPotPulseScale);
            waitingPotPulseSeconds = Mathf.Max(MinimumWorldSize, waitingPotPulseSeconds);
            previewTemporaryWaitingSlotCount = Mathf.Max(0, previewTemporaryWaitingSlotCount);
        }

        private void OnDrawGizmos()
        {
            if (!showLayoutGizmos)
            {
                return;
            }

            var definition = GetPreviewLevelDefinition();
            var width = GetPreviewGridWidth(definition);
            var height = GetPreviewGridHeight(definition);
            var launchColumnCount = GetPreviewLaunchColumnCount(definition);
            var maxQueueCount = GetPreviewMaxQueueCount(definition);
            var waitingSlotCount = GetPreviewWaitingSlotCount(definition);
            CalculateLayoutValues(width, height, out var previewCellSize, out var previewFlowerScale, out var previewPotSpacing, out var previewQueuedPotSpacing);

            DrawBoardGizmos(width, height, previewCellSize, previewFlowerScale);
            DrawConveyorGizmos(width, height, previewCellSize);
            DrawLaunchColumnGizmos(launchColumnCount, maxQueueCount, previewPotSpacing, previewQueuedPotSpacing);
            DrawWaitingSlotGizmos(waitingSlotCount, previewPotSpacing);
            DrawTemporaryWaitingSlotGizmos(previewTemporaryWaitingSlotCount, previewPotSpacing);
            DrawLayoutLabels(previewCellSize, previewFlowerScale, previewPotSpacing, previewQueuedPotSpacing);
        }

        private void BuildFlowerViews()
        {
            for (var y = 0; y < _session.FlowerGrid.Height; y++)
            {
                for (var x = 0; x < _session.FlowerGrid.Width; x++)
                {
                    var cell = _session.FlowerGrid.GetCell(new GridPosition(x, y));
                    if (cell == null || !cell.IsOccupied)
                    {
                        continue;
                    }

                    var view = CreateView(flowerPrefab, flowerRoot, "Flower");
                    view.transform.position = FlowerPosition(cell.Position);
                    ApplyFlowerSprite(view, cell.Color);
                    ApplyVisualSize(view, _runtimeFlowerScale);
                    _flowerViews[cell] = view;
                }
            }
        }

        private void BuildLaunchColumnViews()
        {
            for (var columnIndex = 0; columnIndex < _session.LaunchColumns.Count; columnIndex++)
            {
                RefreshLaunchColumnView(columnIndex);
            }

            RefreshLinkedPotViews();
        }

        private void BuildWaitingSlotViews()
        {
            for (var slotIndex = 0; slotIndex < _session.WaitingSlots.Capacity; slotIndex++)
            {
                CreateWaitingSlotView(slotIndex, false);
            }
        }

        public void ShowLaunchedPot(PotModel pot, int columnIndex)
        {
            if (pot == null)
            {
                return;
            }

            var view = CreateView(potPrefab, conveyorRoot, $"ActivePot_{columnIndex}");
            view.transform.position = LaunchPosition(columnIndex, 0);
            ApplyVisualSize(view, potVisualSize);
            ConfigurePotView(view, pot);
            RegisterActivePotView(pot, view);
            RefreshLaunchColumnView(columnIndex, true);
            RefreshLinkedPotViews();
            UpdateConveyorPotViews(pot, launchMoveSeconds, launchMoveEase);
        }

        public void ShowRecalledPot(PotModel pot, int slotIndex)
        {
            if (pot == null)
            {
                return;
            }

            if (!_potViews.TryGetValue(pot, out var view))
            {
                view = CreateView(potPrefab, conveyorRoot, $"RecalledPot_{slotIndex}");
                ApplyVisualSize(view, potVisualSize);
                ConfigurePotView(view, pot);
                RegisterActivePotView(pot, view);
            }

            UpdatePotCounter(view, pot);
            view.transform.SetParent(conveyorRoot, true);
            UpdateConveyorPotViews(pot, launchMoveSeconds, launchMoveEase);
        }

        private void RefreshLaunchColumnView(int columnIndex, bool animateShift = false)
        {
            if (animateShift && _launchColumnViews.TryGetValue(columnIndex, out var existingViews))
            {
                RefreshLaunchColumnViewWithShift(columnIndex, existingViews);
                return;
            }

            ClearLaunchColumnViews(columnIndex);

            var column = _session.LaunchColumns[columnIndex];
            if (!column.CanLaunch)
            {
                return;
            }

            var views = new List<GameObject>();
            var pots = column.Pots;
            for (var queueIndex = 0; queueIndex < pots.Count; queueIndex++)
            {
                var pot = pots[queueIndex];
                var view = CreateView(potPrefab, launchColumnRoot, $"LaunchPot_{columnIndex}_{queueIndex}");
                view.transform.position = LaunchPosition(columnIndex, queueIndex);
                ApplyVisualSize(view, queueIndex == 0 ? potVisualSize : potVisualSize * BackQueuedPotScale);
                ConfigurePotView(view, pot);
                RegisterQueuedPotView(pot, view);
                SetSortingOrder(view, 20 - queueIndex);

                if (queueIndex == 0)
                {
                    var capturedColumn = columnIndex;
                    BindClick(view, () => LaunchColumnRequested?.Invoke(capturedColumn), LevelClickTargetKind.LaunchColumn, capturedColumn);
                }
                else
                {
                    DisableClick(view);
                }

                views.Add(view);
            }

            _launchColumnViews[columnIndex] = views;
            RefreshLinkedPotViews();
        }

        private void RefreshLaunchColumnViewWithShift(int columnIndex, List<GameObject> previousViews)
        {
            var column = _session.LaunchColumns[columnIndex];
            var pots = column.Pots;

            if (previousViews.Count > 0)
            {
                DestroyLaunchColumnView(previousViews[0]);
            }

            if (!column.CanLaunch)
            {
                for (var i = 1; i < previousViews.Count; i++)
                {
                    DestroyLaunchColumnView(previousViews[i]);
                }

                _launchColumnViews.Remove(columnIndex);
                return;
            }

            var views = new List<GameObject>();
            for (var queueIndex = 0; queueIndex < pots.Count; queueIndex++)
            {
                var pot = pots[queueIndex];
                var previousViewIndex = queueIndex + 1;
                var view = previousViewIndex < previousViews.Count
                    ? previousViews[previousViewIndex]
                    : CreateView(potPrefab, launchColumnRoot, $"LaunchPot_{columnIndex}_{queueIndex}");

                view.name = $"LaunchPot_{columnIndex}_{queueIndex}";
                view.transform.SetParent(launchColumnRoot, true);
                ConfigurePotView(view, pot);
                RegisterQueuedPotView(pot, view);
                SetSortingOrder(view, 20 - queueIndex);

                if (queueIndex == 0)
                {
                    var capturedColumn = columnIndex;
                    BindClick(view, () => LaunchColumnRequested?.Invoke(capturedColumn), LevelClickTargetKind.LaunchColumn, capturedColumn);
                }
                else
                {
                    DisableClick(view);
                }

                AnimateLaunchQueueMove(view, LaunchPosition(columnIndex, queueIndex), GetPotVisualSize(queueIndex), launchMoveSeconds, launchMoveEase);
                views.Add(view);
            }

            for (var i = pots.Count + 1; i < previousViews.Count; i++)
            {
                DestroyLaunchColumnView(previousViews[i]);
            }

            _launchColumnViews[columnIndex] = views;
            RefreshLinkedPotViews();
        }

        public void UpdateConveyorPotViews()
        {
            UpdateConveyorPotViews(null, GetConveyorMoveSeconds(), Ease.Linear);
        }

        public void UpdateConveyorPotViews(float durationMultiplier)
        {
            UpdateConveyorPotViews(null, GetConveyorMoveSeconds() * Mathf.Max(0f, durationMultiplier), Ease.Linear);
        }

        private void UpdateConveyorPotViews(PotModel animatedPot, float animatedDuration, Ease animatedEase)
        {
            foreach (var conveyorPot in _session.Conveyor.Pots)
            {
                if (!_potViews.TryGetValue(conveyorPot.Pot, out var view))
                {
                    continue;
                }

                var duration = ReferenceEquals(conveyorPot.Pot, animatedPot) ? animatedDuration : GetConveyorMoveSeconds();
                var ease = ReferenceEquals(conveyorPot.Pot, animatedPot) ? animatedEase : Ease.Linear;
                AnimateConveyorPotMove(view, conveyorPot, duration, ease);
            }

            RefreshLinkedPotViews();
        }

        private void OnFlowerRemoved(FlowerGrid grid, FlowerCell cell)
        {
            if (_flowerViews.TryGetValue(cell, out var view))
            {
                _flowerViews.Remove(cell);

                if (_pendingFlowerTargets.TryGetValue(cell, out var pot) && _potViews.TryGetValue(pot, out var potView))
                {
                    AnimateFlowerIntoPot(view, potView, pot);
                }
                else
                {
                    AnimateDisappear(view, flowerFlySeconds, flowerFlyEase);
                }
            }

            _pendingFlowerTargets.Remove(cell);
        }

        private void OnWaitingPotPlaced(LevelSession session, PotModel pot, int slotIndex)
        {
            if (!_potViews.TryGetValue(pot, out var potView))
            {
                potView = CreateView(potPrefab, waitingSlotRoot, $"WaitingPot_{slotIndex}");
                ApplyVisualSize(potView, potVisualSize);
                ConfigurePotView(potView, pot);
                RegisterActivePotView(pot, potView);
            }

            ConfigurePotView(potView, pot);
            potView.transform.SetParent(waitingSlotRoot, true);
            SetSortingOrder(potView, 30);
            BindClick(potView, () => WaitingPotRecallRequested?.Invoke(slotIndex), LevelClickTargetKind.WaitingSlot, slotIndex);
            _waitingPotViews[slotIndex] = potView;
            AnimatePotMove(potView, WaitingPotPosition(slotIndex), waitingMoveSeconds, waitingMoveEase);
            StartWaitingPotPulse(potView);
            RefreshLinkedPotViews();
        }

        private void OnWaitingPotRecalled(LevelSession session, PotModel pot, int slotIndex)
        {
            if (_waitingPotViews.TryGetValue(slotIndex, out var waitingPotView))
            {
                _waitingPotViews.Remove(slotIndex);
                StopWaitingPotPulse(waitingPotView);
                waitingPotView.transform.SetParent(conveyorRoot, true);
                RefreshLinkedPotViews();
            }
        }

        private void OnTemporaryWaitingSlotCreated(LevelSession session, WaitingSlot slot)
        {
            if (slot == null)
            {
                return;
            }

            CreateWaitingSlotView(slot.Index, true);
            RefreshTemporaryWaitingSlotPositions();
            FitCameraToLevel();
        }

        private void OnTemporaryWaitingSlotRemoved(LevelSession session, int slotIndex)
        {
            if (_waitingSlotViews.TryGetValue(slotIndex, out var waitingSlotView))
            {
                _waitingSlotViews.Remove(slotIndex);
                AnimateDisappear(waitingSlotView, potDisappearSeconds, potDisappearEase);
            }

            RefreshTemporaryWaitingSlotPositions();
            FitCameraToLevel();
        }

        private void OnConveyorPotRemoved(ConveyorBelt conveyor, ConveyorPot conveyorPot)
        {
            if (!ShouldRemovePotViewAfterConveyorRemoval(conveyorPot.Pot))
            {
                return;
            }

            if (_potViews.TryGetValue(conveyorPot.Pot, out var view))
            {
                _potViews.Remove(conveyorPot.Pot);
                _collectedFlowerCounts.Remove(conveyorPot.Pot);
                UntrackPotCounter(conveyorPot.Pot);
                StopWaitingPotPulse(view);
                AnimateDisappear(view, potDisappearSeconds, potDisappearEase);
                RefreshLinkedPotViews();
            }
        }

        private bool ShouldRemovePotViewAfterConveyorRemoval(PotModel pot)
        {
            if (pot == null || !pot.IsFull)
            {
                return false;
            }

            if (!pot.IsLinked)
            {
                return true;
            }

            var hasLinkedView = false;
            foreach (var candidate in _potViews.Keys)
            {
                if (candidate == null || candidate.LinkedPairId != pot.LinkedPairId)
                {
                    continue;
                }

                hasLinkedView = true;
                if (!candidate.IsFull)
                {
                    return false;
                }
            }

            return hasLinkedView;
        }

        private void RegisterActivePotView(PotModel pot, GameObject view)
        {
            if (pot == null || view == null)
            {
                return;
            }

            _potViews[pot] = view;
            TrackPotCounter(pot);
            UpdatePotCounter(view, pot);
        }

        private void TrackPotCounter(PotModel pot)
        {
            if (pot != null && _trackedPotCounters.Add(pot))
            {
                pot.StateChanged += OnPotStateChanged;
            }
        }

        private void UntrackPotCounter(PotModel pot)
        {
            if (pot != null && _trackedPotCounters.Remove(pot))
            {
                pot.StateChanged -= OnPotStateChanged;
            }
        }

        private void OnPotStateChanged(PotModel pot, PotState previousState, PotState nextState)
        {
            if (pot != null && _potViews.TryGetValue(pot, out var view))
            {
                UpdatePotCounter(view, pot);
                if (nextState == PotState.Broken)
                {
                    ShowFailResolution(pot);
                }
            }
        }

        private void ConfigurePotView(GameObject view, PotModel pot)
        {
            if (view == null || pot == null)
            {
                return;
            }

            SetColor(view, ToUnityColor(pot.Color));
            UpdatePotCounter(view, pot);
        }

        private void RegisterQueuedPotView(PotModel pot, GameObject view)
        {
            if (pot == null || view == null)
            {
                return;
            }

            UnregisterQueuedPotView(view);
            _queuedPotViews[pot] = view;
        }

        private void UnregisterQueuedPotView(GameObject view)
        {
            if (view == null || _queuedPotViews.Count == 0)
            {
                return;
            }

            var potToRemove = default(PotModel);
            foreach (var pair in _queuedPotViews)
            {
                if (pair.Value == view)
                {
                    potToRemove = pair.Key;
                    break;
                }
            }

            if (potToRemove != null)
            {
                _queuedPotViews.Remove(potToRemove);
            }
        }

        private void UpdatePotCounter(GameObject view, PotModel pot)
        {
            var counter = GetPotCounter(view);
            if (counter != null && pot != null)
            {
                counter.text = pot.RemainingCapacity.ToString();
            }
        }

        private TMP_Text GetPotCounter(GameObject view)
        {
            var counter = view != null ? view.GetComponentInChildren<TMP_Text>(true) : null;
            if (counter == null)
            {
                return null;
            }

            counter.gameObject.SetActive(true);
            counter.enabled = true;

            var canvas = counter.GetComponentInParent<Canvas>(true);
            if (canvas != null)
            {
                canvas.gameObject.SetActive(true);
                canvas.enabled = true;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100;
            }

            return counter;
        }

        private void LateUpdate()
        {
            RefreshLinkedPotViews();
        }

        private void RefreshLinkedPotViews()
        {
            if (_session == null)
            {
                return;
            }

            var linkedViews = new Dictionary<int, List<LinkedPotView>>();
            CollectLaunchLinkedViews(linkedViews);
            CollectLiveLinkedViews(linkedViews);

            var activeLinkIds = new HashSet<int>();
            foreach (var pair in linkedViews)
            {
                if (pair.Value.Count < 2)
                {
                    continue;
                }

                activeLinkIds.Add(pair.Key);
                var linkView = GetOrCreateLinkedPotView(pair.Key);
                var lineRenderer = linkView.GetComponent<LineRenderer>();
                UpdateLinkedPotLine(lineRenderer, pair.Value);
            }

            RemoveStaleLinkedPotViews(activeLinkIds);
        }

        private void CollectLaunchLinkedViews(Dictionary<int, List<LinkedPotView>> linkedViews)
        {
            for (var columnIndex = 0; columnIndex < _session.LaunchColumns.Count; columnIndex++)
            {
                if (!_launchColumnViews.TryGetValue(columnIndex, out var views))
                {
                    continue;
                }

                var pots = _session.LaunchColumns[columnIndex].Pots;
                for (var queueIndex = 0; queueIndex < pots.Count && queueIndex < views.Count; queueIndex++)
                {
                    var pot = pots[queueIndex];
                    if (!pot.IsLinked || views[queueIndex] == null)
                    {
                        continue;
                    }

                    if (!linkedViews.TryGetValue(pot.LinkedPairId, out var pairViews))
                    {
                        pairViews = new List<LinkedPotView>();
                        linkedViews[pot.LinkedPairId] = pairViews;
                    }

                    pairViews.Add(new LinkedPotView(pot, views[queueIndex]));
                }
            }
        }

        private void CollectLiveLinkedViews(Dictionary<int, List<LinkedPotView>> linkedViews)
        {
            foreach (var pair in _potViews)
            {
                var pot = pair.Key;
                var view = pair.Value;
                if (pot == null || !pot.IsLinked || view == null)
                {
                    continue;
                }

                if (!linkedViews.TryGetValue(pot.LinkedPairId, out var pairViews))
                {
                    pairViews = new List<LinkedPotView>();
                    linkedViews[pot.LinkedPairId] = pairViews;
                }

                if (!ContainsLinkedPotView(pairViews, view))
                {
                    pairViews.Add(new LinkedPotView(pot, view));
                }
            }
        }

        private static bool ContainsLinkedPotView(List<LinkedPotView> pairViews, GameObject view)
        {
            for (var i = 0; i < pairViews.Count; i++)
            {
                if (pairViews[i].View == view)
                {
                    return true;
                }
            }

            return false;
        }

        private static void UpdateLinkedPotLine(LineRenderer lineRenderer, IReadOnlyList<LinkedPotView> linkedViews)
        {
            if (lineRenderer == null || linkedViews == null || linkedViews.Count < 2)
            {
                return;
            }

            lineRenderer.positionCount = linkedViews.Count;
            for (var i = 0; i < linkedViews.Count; i++)
            {
                if (linkedViews[i].View == null)
                {
                    return;
                }

                lineRenderer.SetPosition(i, linkedViews[i].View.transform.position + Vector3.forward * 0.02f);
            }

            lineRenderer.colorGradient = CreateLinkedPotGradient(linkedViews);
        }

        private static Gradient CreateLinkedPotGradient(IReadOnlyList<LinkedPotView> linkedViews)
        {
            var gradient = new Gradient();
            var colorKeys = new List<GradientColorKey>();
            var alphaKeys = new List<GradientAlphaKey>();
            var segmentCount = linkedViews.Count - 1;

            for (var i = 0; i < segmentCount; i++)
            {
                var firstColor = ToUnityColor(linkedViews[i].Pot.Color);
                var secondColor = ToUnityColor(linkedViews[i + 1].Pot.Color);
                var segmentStart = i / (float)segmentCount;
                var segmentEnd = (i + 1) / (float)segmentCount;
                var segmentMidpoint = (segmentStart + segmentEnd) * 0.5f;

                colorKeys.Add(new GradientColorKey(firstColor, segmentStart));
                colorKeys.Add(new GradientColorKey(firstColor, segmentMidpoint));
                colorKeys.Add(new GradientColorKey(secondColor, segmentMidpoint));
                colorKeys.Add(new GradientColorKey(secondColor, segmentEnd));

                alphaKeys.Add(new GradientAlphaKey(firstColor.a, segmentStart));
                alphaKeys.Add(new GradientAlphaKey(firstColor.a, segmentMidpoint));
                alphaKeys.Add(new GradientAlphaKey(secondColor.a, segmentMidpoint));
                alphaKeys.Add(new GradientAlphaKey(secondColor.a, segmentEnd));
            }

            gradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());

            return gradient;
        }

        private void RemoveStaleLinkedPotViews(HashSet<int> activeLinkIds)
        {
            var staleLinkIds = new List<int>();
            foreach (var pair in _linkedPotViews)
            {
                if (!activeLinkIds.Contains(pair.Key))
                {
                    staleLinkIds.Add(pair.Key);
                }
            }

            for (var i = 0; i < staleLinkIds.Count; i++)
            {
                var linkId = staleLinkIds[i];
                if (_linkedPotViews.TryGetValue(linkId, out var view) && view != null)
                {
                    Destroy(view);
                }

                _linkedPotViews.Remove(linkId);
            }
        }

        private void ClearLinkedPotViews()
        {
            foreach (var view in _linkedPotViews.Values)
            {
                if (view != null)
                {
                    Destroy(view);
                }
            }

            _linkedPotViews.Clear();
        }

        private GameObject GetOrCreateLinkedPotView(int linkedPairId)
        {
            if (_linkedPotViews.TryGetValue(linkedPairId, out var existingView) && existingView != null)
            {
                return existingView;
            }

            var view = new GameObject($"LinkedPotVine_{linkedPairId}");
            view.transform.SetParent(transform, false);

            var lineRenderer = view.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = potVisualSize * 0.28f;
            lineRenderer.endWidth = potVisualSize * 0.28f;
            lineRenderer.numCapVertices = 6;
            lineRenderer.sortingOrder = 25;
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                lineRenderer.material = new Material(shader);
            }

            lineRenderer.startColor = new Color(0.16f, 0.55f, 0.18f, 0.95f);
            lineRenderer.endColor = new Color(0.12f, 0.42f, 0.12f, 0.95f);

            _linkedPotViews[linkedPairId] = view;
            return view;
        }

        private readonly struct LinkedPotView
        {
            public LinkedPotView(PotModel pot, GameObject view)
            {
                Pot = pot;
                View = view;
            }

            public PotModel Pot { get; }
            public GameObject View { get; }
        }

        private GameObject CreateView(GameObject prefab, Transform parent, string objectName)
        {
            GameObject view;
            if (prefab != null)
            {
                view = Instantiate(prefab, parent);
            }
            else
            {
                view = new GameObject(objectName);
                view.transform.SetParent(parent, false);
                view.AddComponent<SpriteRenderer>().sprite = GetFallbackSprite();
            }

            view.name = objectName;
            return view;
        }

        private GameObject CreateWaitingSlotView(int slotIndex, bool isTemporary)
        {
            if (_waitingSlotViews.TryGetValue(slotIndex, out var existingView) && existingView != null)
            {
                return existingView;
            }

            var view = CreateView(waitingSlotPrefab, waitingSlotRoot, $"WaitingSlot_{slotIndex}");
            view.transform.position = WaitingSlotPosition(slotIndex);
            ApplyVisualSize(view, waitingSlotVisualSize);
            SetAlpha(view, isTemporary ? temporaryWaitingSlotAlpha : 1f);
            var capturedSlot = slotIndex;
            BindClick(view, () => WaitingPotRecallRequested?.Invoke(capturedSlot), LevelClickTargetKind.WaitingSlot, capturedSlot);
            _waitingSlotViews[slotIndex] = view;
            return view;
        }

        private void ApplyFlowerSprite(GameObject view, FlowerColor color)
        {
            var spriteRenderer = GetSpriteRenderer(view);
            if (spriteRenderer == null)
            {
                return;
            }

            var sprite = GetFlowerSprite(color);
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }

            spriteRenderer.color = Color.white;
        }

        private Sprite GetFlowerSprite(FlowerColor color)
        {
            if (flowerSprites == null)
            {
                return null;
            }

            for (var i = 0; i < flowerSprites.Length; i++)
            {
                if (flowerSprites[i] != null && flowerSprites[i].Color == color)
                {
                    return flowerSprites[i].Sprite;
                }
            }

            return null;
        }

        public void TrackPotentialFlowerTarget(PotModel pot)
        {
            if (pot == null ||
                !_session.Conveyor.TryGetPot(pot, out var conveyorPot) ||
                !conveyorPot.TryGetCurrentScan(out var scan) ||
                !_session.FlowerGrid.TryFindFirstAccessible(scan, pot.Color, out var flower))
            {
                return;
            }

            _pendingFlowerTargets[flower] = pot;
        }

        private void EnsureRoots()
        {
            flowerRoot = EnsureRoot(flowerRoot, "LevelFlowers");
            conveyorRoot = EnsureRoot(conveyorRoot, "LevelConveyor");
            launchColumnRoot = EnsureRoot(launchColumnRoot, "LevelLaunchColumns");
            waitingSlotRoot = EnsureRoot(waitingSlotRoot, "LevelWaitingSlots");
        }

        private Transform EnsureRoot(Transform root, string rootName)
        {
            if (root != null)
            {
                return root;
            }

            var rootObject = new GameObject(rootName);
            rootObject.transform.SetParent(transform, false);
            return rootObject.transform;
        }

        private static void ClearRoot(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                child.DOKill();
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private void CalculateRuntimeLayout()
        {
            CalculateLayoutValues(
                _session.FlowerGrid.Width,
                _session.FlowerGrid.Height,
                out _runtimeCellSize,
                out _runtimeFlowerScale,
                out _runtimePotSpacing,
                out _runtimeQueuedPotVerticalSpacing);
        }

        private void CalculateLayoutValues(
            int gridWidth,
            int gridHeight,
            out float layoutCellSize,
            out float layoutFlowerScale,
            out float layoutPotSpacing,
            out float layoutQueuedPotVerticalSpacing)
        {
            var largestGridAxis = Mathf.Max(1, Mathf.Max(gridWidth, gridHeight));
            layoutCellSize = largestGridAxis <= 1
                ? cellSize
                : cellSize * (referenceGridAxis - 1) / (float)(largestGridAxis - 1);
            layoutFlowerScale = layoutCellSize * flowerScaleMultiplier;
            layoutPotSpacing = Mathf.Max(potSpacing, GetMinimumPotSpacing());
            layoutQueuedPotVerticalSpacing = Mathf.Max(queuedPotVerticalSpacing, GetMinimumQueuedPotSpacing());
        }

        private float GetMinimumPotSpacing()
        {
            return Mathf.Max(potVisualSize, waitingSlotVisualSize) + minimumVisualGap;
        }

        private float GetMinimumQueuedPotSpacing()
        {
            var frontPotRadius = potVisualSize * 0.5f;
            var backPotRadius = potVisualSize * BackQueuedPotScale * 0.5f;
            return frontPotRadius + backPotRadius + minimumVisualGap;
        }

        private float GetConveyorMoveSeconds()
        {
            return Mathf.Max(ScaleDurationForGrid(conveyorStepSeconds), ScaleDurationForGrid(conveyorMoveSeconds));
        }

        private float ScaleDurationForGrid(float duration)
        {
            return Mathf.Max(0f, duration) * GetGridTimeScale();
        }

        private float GetGridTimeScale()
        {
            if (_session == null)
            {
                return 1f;
            }

            var largestGridAxis = Mathf.Max(1, Mathf.Max(_session.FlowerGrid.Width, _session.FlowerGrid.Height));
            if (largestGridAxis <= 1)
            {
                return 1f;
            }

            return (referenceGridAxis - 1) / (float)(largestGridAxis - 1);
        }

        private void FitCameraToLevel()
        {
            if (!autoFitCamera || _camera == null || !_camera.orthographic)
            {
                return;
            }

            var maxQueueCount = 0;
            for (var i = 0; i < _session.LaunchColumns.Count; i++)
            {
                maxQueueCount = Mathf.Max(maxQueueCount, _session.LaunchColumns[i].Count);
            }

            var conveyorGap = GetConveyorPathGap();
            var waitingMinX = waitingOrigin.x - waitingSlotVisualSize;
            var waitingMaxX = waitingOrigin.x + waitingSlotVisualSize;
            var waitingMinY = waitingOrigin.y - Mathf.Max(0, _session.WaitingSlots.Capacity - 1) * _runtimePotSpacing - waitingSlotVisualSize;

            if (HasTemporaryWaitingSlots())
            {
                var temporaryOrigin = TemporaryWaitingOrigin();
                waitingMinX = Mathf.Min(waitingMinX, temporaryOrigin.x - waitingSlotVisualSize);
                waitingMaxX = Mathf.Max(waitingMaxX, temporaryOrigin.x + waitingSlotVisualSize);
                waitingMinY = Mathf.Min(waitingMinY, temporaryOrigin.y - GetTemporaryWaitingSlotLayoutDepth() * _runtimePotSpacing - waitingSlotVisualSize);
            }

            var minX = Mathf.Min(
                flowerOrigin.x - conveyorGap,
                launchOrigin.x - potVisualSize,
                waitingMinX);
            var maxX = Mathf.Max(
                flowerOrigin.x + (_session.FlowerGrid.Width - 1) * _runtimeCellSize + conveyorGap,
                launchOrigin.x + (_session.LaunchColumns.Count - 1) * _runtimePotSpacing + potVisualSize,
                waitingMaxX);
            var minY = Mathf.Min(
                flowerOrigin.y - conveyorGap,
                launchOrigin.y - Mathf.Max(0, maxQueueCount - 1) * _runtimeQueuedPotVerticalSpacing - potVisualSize,
                waitingMinY);
            var maxY = flowerOrigin.y + (_session.FlowerGrid.Height - 1) * _runtimeCellSize + conveyorGap;

            var center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, _camera.transform.position.z);
            var worldWidth = maxX - minX + cameraPadding * 2f;
            var worldHeight = maxY - minY + cameraPadding * 2f;

            _camera.transform.position = center;
            _camera.orthographicSize = Mathf.Max(worldHeight * 0.5f, worldWidth / Mathf.Max(_camera.aspect, 0.01f) * 0.5f);
        }

        private void BindClick(GameObject view, Action clicked, LevelClickTargetKind kind, int index)
        {
            var boxCollider = view.GetComponent<BoxCollider2D>();
            if (boxCollider == null)
            {
                boxCollider = view.AddComponent<BoxCollider2D>();
            }

            var spriteRenderer = GetSpriteRenderer(view);
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                var spriteBounds = spriteRenderer.sprite.bounds;
                boxCollider.size = spriteBounds.size * 1.15f;
                boxCollider.offset = spriteBounds.center;
            }

            var colliders = view.GetComponents<Collider2D>();
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = true;
            }

            var target = view.GetComponent<LevelClickTarget>();
            if (target == null)
            {
                target = view.AddComponent<LevelClickTarget>();
            }

            target.Bind(clicked);
            target.SetTarget(kind, index);
        }

        private void DisableClick(GameObject view)
        {
            var colliders = view.GetComponents<Collider2D>();
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private void SetColor(GameObject view, Color color)
        {
            var spriteRenderer = GetSpriteRenderer(view);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
                return;
            }

            var renderer = view.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private void SetAlpha(GameObject view, float alpha)
        {
            alpha = Mathf.Clamp01(alpha);

            var spriteRenderers = view.GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                var color = spriteRenderers[i].color;
                color.a = alpha;
                spriteRenderers[i].color = color;
            }

            var renderers = view.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is SpriteRenderer)
                {
                    continue;
                }

                var color = renderers[i].material.color;
                color.a = alpha;
                renderers[i].material.color = color;
            }
        }

        private void SetSortingOrder(GameObject view, int sortingOrder)
        {
            var spriteRenderer = GetSpriteRenderer(view);
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = sortingOrder;
            }
        }

        private void ApplyVisualSize(GameObject view, float targetWorldSize)
        {
            view.transform.localScale = GetVisualScale(view, targetWorldSize);
        }

        private Vector3 GetVisualScale(GameObject view, float targetWorldSize)
        {
            var spriteRenderer = GetSpriteRenderer(view);
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                var spriteSize = spriteRenderer.sprite.bounds.size;
                var largestAxis = Mathf.Max(spriteSize.x, spriteSize.y);
                if (largestAxis > 0f)
                {
                    var scale = targetWorldSize / largestAxis;
                    return Vector3.one * scale;
                }
            }

            return Vector3.one * targetWorldSize;
        }

        private SpriteRenderer GetSpriteRenderer(GameObject view)
        {
            var spriteRenderer = view.GetComponent<SpriteRenderer>();
            return spriteRenderer != null ? spriteRenderer : view.GetComponentInChildren<SpriteRenderer>();
        }

        private Sprite GetFallbackSprite()
        {
            if (_fallbackSprite != null)
            {
                return _fallbackSprite;
            }

            var texture = new Texture2D(1, 1)
            {
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            _fallbackSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);

            return _fallbackSprite;
        }

        private Vector3 FlowerPosition(GridPosition position)
        {
            return new Vector3(
                flowerOrigin.x + position.X * _runtimeCellSize,
                flowerOrigin.y + position.Y * _runtimeCellSize,
                0f);
        }

        private void ClearLaunchColumnViews(int columnIndex)
        {
            if (!_launchColumnViews.TryGetValue(columnIndex, out var views))
            {
                return;
            }

            for (var i = 0; i < views.Count; i++)
            {
                DestroyLaunchColumnView(views[i]);
            }

            _launchColumnViews.Remove(columnIndex);
        }

        private void DestroyLaunchColumnView(GameObject view)
        {
            if (view == null)
            {
                return;
            }

            view.transform.DOKill();
            UnregisterQueuedPotView(view);
            Destroy(view);
        }

        private Vector3 LaunchPosition(int columnIndex, int queueIndex)
        {
            return new Vector3(
                launchOrigin.x + columnIndex * _runtimePotSpacing,
                launchOrigin.y - queueIndex * _runtimeQueuedPotVerticalSpacing,
                queueIndex * 0.01f);
        }

        private float GetPotVisualSize(int queueIndex)
        {
            return queueIndex == 0 ? potVisualSize : potVisualSize * BackQueuedPotScale;
        }

        private Vector3 WaitingSlotPosition(int slotIndex)
        {
            if (IsTemporaryWaitingSlot(slotIndex))
            {
                var temporaryIndex = GetTemporaryWaitingSlotLayoutIndex(slotIndex);
                var origin = TemporaryWaitingOrigin();
                return new Vector3(origin.x, origin.y - temporaryIndex * _runtimePotSpacing, 0f);
            }

            return new Vector3(waitingOrigin.x, waitingOrigin.y - slotIndex * _runtimePotSpacing, 0f);
        }

        private Vector3 WaitingPotPosition(int slotIndex)
        {
            var slotPosition = WaitingSlotPosition(slotIndex);
            return new Vector3(slotPosition.x, slotPosition.y, -0.01f);
        }

        private Vector3 ConveyorPosition(ConveyorPot conveyorPot)
        {
            if (!conveyorPot.TryGetCurrentScan(out var scan))
            {
                return new Vector3(waitingOrigin.x, waitingOrigin.y - _session.WaitingSlots.Capacity * _runtimePotSpacing, 0f);
            }

            return ConveyorScanPosition(scan, _session.FlowerGrid.Width, _session.FlowerGrid.Height, _runtimeCellSize);
        }

        private Vector2 TemporaryWaitingOrigin()
        {
            return new Vector2(
                waitingOrigin.x + temporaryWaitingSlotOffset.x,
                waitingOrigin.y + temporaryWaitingSlotOffset.y);
        }

        private bool IsTemporaryWaitingSlot(int slotIndex)
        {
            return _session != null && slotIndex >= _session.WaitingSlots.Capacity;
        }

        private bool HasTemporaryWaitingSlots()
        {
            if (_session == null)
            {
                return false;
            }

            var slots = _session.WaitingSlots.Slots;
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsTemporary)
                {
                    return true;
                }
            }

            return false;
        }

        private int GetTemporaryWaitingSlotLayoutDepth()
        {
            if (_session == null)
            {
                return 0;
            }

            var temporarySlotCount = 0;
            var slots = _session.WaitingSlots.Slots;
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsTemporary)
                {
                    temporarySlotCount++;
                }
            }

            return Mathf.Max(0, temporarySlotCount - 1);
        }

        private int GetTemporaryWaitingSlotLayoutIndex(int slotIndex)
        {
            if (_session == null)
            {
                return 0;
            }

            var layoutIndex = 0;
            var slots = _session.WaitingSlots.Slots;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsTemporary && slot.Index < slotIndex)
                {
                    layoutIndex++;
                }
            }

            return layoutIndex;
        }

        private void RefreshTemporaryWaitingSlotPositions()
        {
            if (_session == null)
            {
                return;
            }

            var slots = _session.WaitingSlots.Slots;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsTemporary)
                {
                    continue;
                }

                if (_waitingSlotViews.TryGetValue(slot.Index, out var slotView))
                {
                    AnimatePotMove(slotView, WaitingSlotPosition(slot.Index), waitingMoveSeconds, waitingMoveEase);
                }

                if (slot.Pot != null && _waitingPotViews.TryGetValue(slot.Index, out var potView))
                {
                    AnimatePotMove(potView, WaitingPotPosition(slot.Index), waitingMoveSeconds, waitingMoveEase);
                    StartWaitingPotPulse(potView);
                }
            }
        }

        private Vector3 ConveyorScanPosition(ConveyorScan scan, int gridWidth, int gridHeight, float layoutCellSize)
        {
            var conveyorGap = GetConveyorPathGap();
            var xMin = flowerOrigin.x - conveyorGap;
            var xMax = flowerOrigin.x + (gridWidth - 1) * layoutCellSize + conveyorGap;
            var yMin = flowerOrigin.y - conveyorGap;
            var yMax = flowerOrigin.y + (gridHeight - 1) * layoutCellSize + conveyorGap;

            return scan.Edge switch
            {
                ConveyorEdge.Left => new Vector3(xMin, EdgeLanePosition(scan.Lane, gridHeight, yMin, yMax), 0f),
                ConveyorEdge.Top => new Vector3(EdgeLanePosition(scan.Lane, gridWidth, xMin, xMax), yMax, 0f),
                ConveyorEdge.Right => new Vector3(xMax, EdgeLanePosition(scan.Lane, gridHeight, yMin, yMax), 0f),
                ConveyorEdge.Bottom => new Vector3(EdgeLanePosition(scan.Lane, gridWidth, xMin, xMax), yMin, 0f),
                _ => transform.position
            };
        }

        private static float EdgeLanePosition(int lane, int laneCount, float min, float max)
        {
            if (laneCount <= 1)
            {
                return (min + max) * 0.5f;
            }

            return Mathf.Lerp(min, max, lane / (float)(laneCount - 1));
        }

        private void AnimateConveyorPotMove(GameObject view, ConveyorPot conveyorPot, float duration, Ease ease)
        {
            AnimatePotMove(view, ConveyorPosition(conveyorPot), duration, ease);
        }

        private void StartWaitingPotPulse(GameObject view)
        {
            if (view == null)
            {
                return;
            }

            StopWaitingPotPulse(view);

            var baseScale = GetVisualScale(view, potVisualSize);
            view.transform.localScale = baseScale;
            var pulseTween = view.transform
                .DOScale(baseScale * waitingPotPulseScale, waitingPotPulseSeconds)
                .SetDelay(waitingMoveSeconds)
                .SetEase(waitingPotPulseEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(view, LinkBehaviour.KillOnDestroy);

            _waitingPotPulseTweens[view] = pulseTween;
        }

        private void StopWaitingPotPulse(GameObject view)
        {
            if (view == null)
            {
                return;
            }

            if (_waitingPotPulseTweens.TryGetValue(view, out var pulseTween))
            {
                pulseTween.Kill();
                _waitingPotPulseTweens.Remove(view);
            }

            view.transform.localScale = GetVisualScale(view, potVisualSize);
        }

        private void AnimatePotMove(GameObject view, Vector3 targetPosition, float duration, Ease ease)
        {
            if (view == null)
            {
                return;
            }

            view.transform.DOKill();
            if (duration <= 0f)
            {
                view.transform.position = targetPosition;
                return;
            }

            view.transform.DOMove(targetPosition, duration)
                .SetEase(ease)
                .SetLink(view, LinkBehaviour.KillOnDestroy);
        }

        private void AnimateLaunchQueueMove(GameObject view, Vector3 targetPosition, float targetWorldSize, float duration, Ease ease)
        {
            if (view == null)
            {
                return;
            }

            var targetScale = GetVisualScale(view, targetWorldSize);
            view.transform.DOKill();
            if (duration <= 0f)
            {
                view.transform.position = targetPosition;
                view.transform.localScale = targetScale;
                return;
            }

            view.transform.DOMove(targetPosition, duration)
                .SetEase(ease)
                .SetLink(view, LinkBehaviour.KillOnDestroy);
            view.transform.DOScale(targetScale, duration)
                .SetEase(ease)
                .SetLink(view, LinkBehaviour.KillOnDestroy);
        }

        private void AnimateFlowerIntoPot(GameObject flowerView, GameObject potView, PotModel pot)
        {
            if (flowerView == null || potView == null || pot == null)
            {
                return;
            }

            flowerView.transform.DOKill();
            var startPosition = flowerView.transform.position;
            var count = _collectedFlowerCounts.TryGetValue(pot, out var currentCount) ? currentCount : 0;
            var potPosition = potView.transform.position + FlowerInPotWorldOffset(count);
            var middlePosition = Vector3.Lerp(startPosition, potPosition, 0.5f) + Vector3.up * flowerFlyArcHeight;
            var path = new[] { middlePosition, potPosition };
            var targetScale = flowerView.transform.localScale * 0.25f;

            var sequence = DOTween.Sequence()
                .SetLink(flowerView, LinkBehaviour.KillOnDestroy);

            sequence.Join(flowerView.transform.DOPath(path, flowerFlySeconds, PathType.CatmullRom)
                .SetEase(flowerFlyEase));
            sequence.Join(flowerView.transform.DOScale(targetScale, flowerFlySeconds)
                .SetEase(Ease.InSine));
            sequence.OnComplete(() => PlaceFlowerInsidePot(flowerView, potView, pot));
        }

        private void PlaceFlowerInsidePot(GameObject flowerView, GameObject potView, PotModel pot)
        {
            if (flowerView == null || potView == null || pot == null)
            {
                return;
            }

            var count = _collectedFlowerCounts.TryGetValue(pot, out var currentCount) ? currentCount : 0;
            _collectedFlowerCounts[pot] = count + 1;

            flowerView.transform.SetParent(potView.transform, false);
            flowerView.transform.localPosition = ToParentLocalOffset(potView.transform, FlowerInPotWorldOffset(count)+Vector3.up*0.1f);
            ApplyVisualSizeInsideParent(flowerView, potVisualSize * 0.3f);
            SetSortingOrder(flowerView, 60 + count);

            var spriteRenderer = GetSpriteRenderer(flowerView);
            if (spriteRenderer != null)
            {
                var color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }
        }

        private static Vector3 FlowerInPotWorldOffset(int collectedIndex)
        {
            return (collectedIndex % 6) switch
            {
                0 => new Vector3(0f, 0.06f, -0.02f),
                1 => new Vector3(-0.055f, 0.02f, -0.02f),
                2 => new Vector3(0.055f, 0.02f, -0.02f),
                3 => new Vector3(-0.028f, 0.08f, -0.02f),
                4 => new Vector3(0.028f, 0.08f, -0.02f),
                _ => new Vector3(0f, 0.02f, -0.02f)
            };
        }

        private static Vector3 ToParentLocalOffset(Transform parent, Vector3 worldOffset)
        {
            if (parent == null)
            {
                return worldOffset;
            }

            var scale = parent.lossyScale;
            return new Vector3(
                SafeDivide(worldOffset.x, scale.x),
                SafeDivide(worldOffset.y, scale.y),
                worldOffset.z);
        }

        private void ApplyVisualSizeInsideParent(GameObject view, float targetWorldSize)
        {
            var spriteRenderer = GetSpriteRenderer(view);
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                var spriteSize = spriteRenderer.sprite.bounds.size;
                var largestAxis = Mathf.Max(spriteSize.x, spriteSize.y);
                var parentScale = GetLargestParentScale(view.transform);
                if (largestAxis > 0f && parentScale > 0f)
                {
                    var scale = targetWorldSize / (largestAxis * parentScale);
                    view.transform.localScale = Vector3.one * scale;
                    return;
                }
            }

            view.transform.localScale = Vector3.one * targetWorldSize;
        }

        private static float GetLargestParentScale(Transform child)
        {
            if (child == null || child.parent == null)
            {
                return 1f;
            }

            var scale = child.parent.lossyScale;
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), MinimumWorldSize);
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) <= MinimumWorldSize ? value : value / divisor;
        }

        private void AnimateDisappear(GameObject view, float duration, Ease ease)
        {
            if (view == null)
            {
                return;
            }

            view.transform.DOKill();
            if (duration <= 0f)
            {
                Destroy(view);
                return;
            }

            var spriteRenderer = GetSpriteRenderer(view);
            var sequence = DOTween.Sequence()
                .SetLink(view, LinkBehaviour.KillOnDestroy);

            sequence.Join(view.transform.DOScale(Vector3.zero, duration).SetEase(ease));
            if (spriteRenderer != null)
            {
                sequence.Join(spriteRenderer.DOFade(0f, duration));
            }

            sequence.OnComplete(() => Destroy(view));
        }

        private LevelDefinition GetPreviewLevelDefinition()
        {
            return BuiltInPrototypeLevelDefinition.Create();
        }

        private static int GetPreviewGridWidth(LevelDefinition definition)
        {
            return Mathf.Max(1, definition?.Width ?? 1);
        }

        private static int GetPreviewGridHeight(LevelDefinition definition)
        {
            return Mathf.Max(1, definition?.Height ?? 1);
        }

        private static int GetPreviewLaunchColumnCount(LevelDefinition definition)
        {
            return Mathf.Max(1, definition?.LaunchColumns?.Length ?? LevelDefinition.DefaultLaunchColumnCount);
        }

        private static int GetPreviewWaitingSlotCount(LevelDefinition definition)
        {
            return Mathf.Max(1, definition?.WaitingSlotCount ?? 5);
        }

        private static int GetPreviewMaxQueueCount(LevelDefinition definition)
        {
            var columns = definition?.LaunchColumns;
            if (columns == null || columns.Length == 0)
            {
                return 1;
            }

            var maxQueueCount = 1;
            for (var i = 0; i < columns.Length; i++)
            {
                maxQueueCount = Mathf.Max(maxQueueCount, columns[i]?.Count ?? 0);
            }

            return maxQueueCount;
        }

        private void DrawBoardGizmos(int width, int height, float previewCellSize, float previewFlowerScale)
        {
            var boardCenter = new Vector3(
                flowerOrigin.x + (width - 1) * previewCellSize * 0.5f,
                flowerOrigin.y + (height - 1) * previewCellSize * 0.5f,
                0f);

            Gizmos.color = new Color(0.35f, 0.8f, 0.55f, 0.75f);
            Gizmos.DrawWireCube(boardCenter, new Vector3(width * previewCellSize, height * previewCellSize, 0.02f));

            Gizmos.color = new Color(0.35f, 0.8f, 0.55f, 0.22f);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var position = new Vector3(flowerOrigin.x + x * previewCellSize, flowerOrigin.y + y * previewCellSize, 0f);
                    Gizmos.DrawWireCube(position, new Vector3(previewCellSize, previewCellSize, 0.01f));

                    if (showVisualFootprintGizmos)
                    {
                        Gizmos.DrawWireCube(position, new Vector3(previewFlowerScale, previewFlowerScale, 0.01f));
                    }
                }
            }
        }

        private void DrawConveyorGizmos(int width, int height, float previewCellSize)
        {
            var conveyorGap = GetConveyorPathGap();
            var xMin = flowerOrigin.x - conveyorGap;
            var xMax = flowerOrigin.x + (width - 1) * previewCellSize + conveyorGap;
            var yMin = flowerOrigin.y - conveyorGap;
            var yMax = flowerOrigin.y + (height - 1) * previewCellSize + conveyorGap;

            var bottomLeft = new Vector3(xMin, yMin, 0f);
            var topLeft = new Vector3(xMin, yMax, 0f);
            var topRight = new Vector3(xMax, yMax, 0f);
            var bottomRight = new Vector3(xMax, yMin, 0f);

            Gizmos.color = new Color(0.25f, 0.55f, 1f, 0.85f);
            Gizmos.DrawLine(bottomLeft, topLeft);
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);

            if (showVisualFootprintGizmos)
            {
                Gizmos.color = new Color(0.25f, 0.55f, 1f, 0.22f);
                Gizmos.DrawWireCube(
                    new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f),
                    new Vector3(xMax - xMin, yMax - yMin, 0.01f));
            }
        }

        private void DrawLaunchColumnGizmos(int columnCount, int maxQueueCount, float previewPotSpacing, float previewQueuedPotSpacing)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.85f);
            for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                for (var queueIndex = 0; queueIndex < maxQueueCount; queueIndex++)
                {
                    var size = queueIndex == 0 ? potVisualSize : potVisualSize * BackQueuedPotScale;
                    var position = new Vector3(
                        launchOrigin.x + columnIndex * previewPotSpacing,
                        launchOrigin.y - queueIndex * previewQueuedPotSpacing,
                        0f);

                    Gizmos.DrawWireCube(position, new Vector3(size, size, 0.01f));
                }
            }

            if (columnCount > 1)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
                var first = new Vector3(launchOrigin.x, launchOrigin.y, 0f);
                var last = new Vector3(launchOrigin.x + (columnCount - 1) * previewPotSpacing, launchOrigin.y, 0f);
                Gizmos.DrawLine(first, last);
            }
        }

        private void DrawWaitingSlotGizmos(int slotCount, float previewPotSpacing)
        {
            Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.75f);
            for (var slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                var position = new Vector3(waitingOrigin.x, waitingOrigin.y - slotIndex * previewPotSpacing, 0f);
                Gizmos.DrawWireCube(position, new Vector3(waitingSlotVisualSize, waitingSlotVisualSize, 0.01f));

                if (showVisualFootprintGizmos)
                {
                    Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
                    Gizmos.DrawWireCube(position, new Vector3(potVisualSize, potVisualSize, 0.01f));
                    Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.75f);
                }
            }

            if (slotCount > 1)
            {
                Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.35f);
                var first = new Vector3(waitingOrigin.x, waitingOrigin.y, 0f);
                var last = new Vector3(waitingOrigin.x, waitingOrigin.y - (slotCount - 1) * previewPotSpacing, 0f);
                Gizmos.DrawLine(first, last);
            }
        }

        private void DrawTemporaryWaitingSlotGizmos(int slotCount, float previewPotSpacing)
        {
            if (slotCount <= 0)
            {
                return;
            }

            var origin = PreviewTemporaryWaitingOrigin();
            Gizmos.color = new Color(0.8f, 0.8f, 0.8f, Mathf.Lerp(0.2f, 0.75f, temporaryWaitingSlotAlpha));
            for (var slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                var position = new Vector3(origin.x, origin.y - slotIndex * previewPotSpacing, 0f);
                Gizmos.DrawWireCube(position, new Vector3(waitingSlotVisualSize, waitingSlotVisualSize, 0.01f));

                if (showVisualFootprintGizmos)
                {
                    Gizmos.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.12f, 0.35f, temporaryWaitingSlotAlpha));
                    Gizmos.DrawWireCube(position, new Vector3(potVisualSize, potVisualSize, 0.01f));
                    Gizmos.color = new Color(0.8f, 0.8f, 0.8f, Mathf.Lerp(0.2f, 0.75f, temporaryWaitingSlotAlpha));
                }
            }

            if (slotCount > 1)
            {
                Gizmos.color = new Color(0.8f, 0.8f, 0.8f, Mathf.Lerp(0.12f, 0.35f, temporaryWaitingSlotAlpha));
                var first = new Vector3(origin.x, origin.y, 0f);
                var last = new Vector3(origin.x, origin.y - (slotCount - 1) * previewPotSpacing, 0f);
                Gizmos.DrawLine(first, last);
            }
        }

        private Vector2 PreviewTemporaryWaitingOrigin()
        {
            return new Vector2(
                waitingOrigin.x + temporaryWaitingSlotOffset.x,
                waitingOrigin.y + temporaryWaitingSlotOffset.y);
        }

        private void DrawLayoutLabels(float previewCellSize, float previewFlowerScale, float previewPotSpacing, float previewQueuedPotSpacing)
        {
#if UNITY_EDITOR
            Handles.color = Color.white;
            Handles.Label(
                new Vector3(flowerOrigin.x, flowerOrigin.y + previewCellSize, 0f),
                $"Cell {previewCellSize:0.##} / Flower {previewFlowerScale:0.##} / Ref {referenceGridAxis}x{referenceGridAxis}");
            Handles.Label(
                new Vector3(flowerOrigin.x - GetConveyorPathGap(), flowerOrigin.y, 0f),
                $"Conveyor gap {conveyorPathGapInCells:0.##} ref cells");
            Handles.Label(
                new Vector3(launchOrigin.x, launchOrigin.y + potVisualSize, 0f),
                $"Pot {potVisualSize:0.##} / H spacing {previewPotSpacing:0.##} / V spacing {previewQueuedPotSpacing:0.##}");
            Handles.Label(
                new Vector3(waitingOrigin.x, waitingOrigin.y + waitingSlotVisualSize, 0f),
                $"Slot {waitingSlotVisualSize:0.##} / Pot footprint {potVisualSize:0.##}");
#endif
        }

        private static Color ToUnityColor(FlowerColor color)
        {
            return color switch
            {
                FlowerColor.Red => Color.red,
                FlowerColor.Yellow => Color.yellow,
                FlowerColor.Blue => Color.blue,
                FlowerColor.Pink => new Color(1f, 0.35f, 0.65f),
                FlowerColor.Purple => new Color(0.55f, 0.25f, 0.9f),
                FlowerColor.White => Color.white,
                FlowerColor.Orange => new Color(1f, 0.5f, 0f),
                FlowerColor.Black => Color.darkGray,
                FlowerColor.Cyan => Color.cyan,
                FlowerColor.Magenta => Color.magenta,
                _ => Color.white
            };
        }

        private float GetConveyorPathGap()
        {
            return Mathf.Max(0f, conveyorPathGapInCells) * cellSize;
        }

        [Serializable]
        private sealed class FlowerSpriteBinding
        {
            [SerializeField] private FlowerColor color;
            [SerializeField] private Sprite sprite;

            public FlowerSpriteBinding(FlowerColor color)
            {
                this.color = color;
            }

            public FlowerColor Color => color;
            public Sprite Sprite => sprite;
        }
    }
}
