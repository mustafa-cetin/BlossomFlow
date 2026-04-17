using System;
using System.Collections.Generic;
using BlossomFlow.Gameplay.Domain;
using BlossomFlow.Gameplay.Domain.Levels;
using DG.Tweening;
using Reflex.Attributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BlossomFlow.Gameplay.Presentation
{
    public sealed class PrototypeLevelRunner : MonoBehaviour
    {
        [Header("Level")]
        [SerializeField] private LevelDefinitionAsset levelAsset;
        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private bool useBuiltInTestLevel = true;

        [Header("Roots")]
        [SerializeField] private Transform flowerRoot;
        [SerializeField] private Transform conveyorRoot;
        [SerializeField] private Transform launchColumnRoot;
        [SerializeField] private Transform waitingSlotRoot;

        [Header("Optional Prefabs")]
        [SerializeField] private GameObject flowerPrefab;
        [SerializeField] private GameObject potPrefab;
        [SerializeField] private GameObject waitingSlotPrefab;

        [Header("Layout")]
        [SerializeField] private Vector2 flowerOrigin = new Vector2(-2f, 0f);
        [SerializeField] private Vector2 launchOrigin = new Vector2(-3.2f, -3.5f);
        [SerializeField] private Vector2 waitingOrigin = new Vector2(-3.2f, -2.35f);
        [SerializeField] private float cellSize = 0.8f;
        [SerializeField] private float maxBoardWorldSize = 5.6f;
        [SerializeField] private float flowerScaleMultiplier = 0.82f;
        [SerializeField] private float potVisualSize = 0.34f;
        [SerializeField] private float waitingSlotVisualSize = 0.42f;
        [SerializeField] private float potSpacing = 0.75f;
        [SerializeField] private float queuedPotVerticalSpacing = 0.46f;
        [SerializeField] private float minimumVisualGap = 0.06f;
        [SerializeField] private float conveyorStepSeconds = 0.45f;
        [SerializeField] private bool autoFitCamera = true;
        [SerializeField] private float cameraPadding = 0.6f;

        [Header("Animation")]
        [SerializeField] private float conveyorMoveSeconds = 0.45f;
        [SerializeField] private float launchMoveSeconds = 0.38f;
        [SerializeField] private float waitingMoveSeconds = 0.35f;
        [SerializeField] private float flowerFlySeconds = 0.22f;
        [SerializeField] private float potDisappearSeconds = 0.24f;
        [SerializeField] private float flowerFlyArcHeight = 0.35f;
        [SerializeField] private Ease launchMoveEase = Ease.OutCubic;
        [SerializeField] private Ease waitingMoveEase = Ease.OutCubic;
        [SerializeField] private Ease flowerFlyEase = Ease.InCubic;
        [SerializeField] private Ease potDisappearEase = Ease.InBack;

        [Header("Editor Preview")]
        [SerializeField] private bool showLayoutGizmos = true;
        [SerializeField] private bool showVisualFootprintGizmos = true;

        private readonly Dictionary<FlowerCell, GameObject> _flowerViews = new();
        private readonly Dictionary<PotModel, GameObject> _potViews = new();
        private readonly Dictionary<int, GameObject> _waitingPotViews = new();
        private readonly Dictionary<int, List<GameObject>> _launchColumnViews = new();
        private readonly Dictionary<FlowerCell, PotModel> _pendingFlowerTargets = new();
        private readonly Dictionary<PotModel, int> _collectedFlowerCounts = new();
        private readonly List<GameObject> _waitingSlotViews = new();

        private ILevelSessionFactory _sessionFactory;
        private LevelSession _session;
        private Sprite _fallbackSprite;
        private float _stepTimer;
        private Camera _camera;
        private float _runtimeCellSize;
        private float _runtimeFlowerScale;
        private float _runtimePotSpacing;
        private float _runtimeQueuedPotVerticalSpacing;

        private const float MinimumWorldSize = 0.01f;
        private const float BackQueuedPotScale = 0.82f;

        [Inject]
        private void Construct(ILevelSessionFactory sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        private void Start()
        {
            if (_sessionFactory == null)
            {
                Debug.LogError("PrototypeLevelRunner needs Reflex injection. Add a SceneScope with GameInstaller to this scene.");
                enabled = false;
                return;
            }

            var definition = useBuiltInTestLevel
                ? CreateBuiltInTestLevel()
                : levelAsset != null
                    ? levelAsset.Definition
                    : levelDefinition ?? CreateBuiltInTestLevel();

            _session = _sessionFactory.Create(definition);
            EnsureRoots();
            CalculateRuntimeLayout();
            _camera = Camera.main;
            FitCameraToPrototype();
            _session.FlowerGrid.FlowerRemoved += OnFlowerRemoved;
            _session.WaitingPotPlaced += OnWaitingPotPlaced;
            _session.WaitingPotRecalled += OnWaitingPotRecalled;
            _session.LevelWon += OnLevelWon;
            _session.LevelFailed += OnLevelFailed;
            _session.Conveyor.PotRemoved += OnConveyorPotRemoved;
            _session.Start();

            BuildFlowerViews();
            BuildLaunchColumnViews();
            BuildWaitingSlotViews();
        }

        private void Update()
        {
            HandlePointerInput();

            if (_session == null || _session.State != LevelPlayState.Playing)
            {
                return;
            }

            _stepTimer += Time.deltaTime;
            if (_stepTimer < conveyorStepSeconds)
            {
                return;
            }

            _stepTimer = 0f;
            TickActivePots();
        }

        private void OnValidate()
        {
            cellSize = Mathf.Max(MinimumWorldSize, cellSize);
            maxBoardWorldSize = Mathf.Max(MinimumWorldSize, maxBoardWorldSize);
            flowerScaleMultiplier = Mathf.Max(MinimumWorldSize, flowerScaleMultiplier);
            potVisualSize = Mathf.Max(MinimumWorldSize, potVisualSize);
            waitingSlotVisualSize = Mathf.Max(MinimumWorldSize, waitingSlotVisualSize);
            minimumVisualGap = Mathf.Max(0f, minimumVisualGap);
            potSpacing = Mathf.Max(Mathf.Max(MinimumWorldSize, potSpacing), GetMinimumPotSpacing());
            queuedPotVerticalSpacing = Mathf.Max(Mathf.Max(MinimumWorldSize, queuedPotVerticalSpacing), GetMinimumQueuedPotSpacing());
            conveyorStepSeconds = Mathf.Max(MinimumWorldSize, conveyorStepSeconds);
            cameraPadding = Mathf.Max(0f, cameraPadding);
            conveyorMoveSeconds = Mathf.Max(conveyorStepSeconds, conveyorMoveSeconds);
            launchMoveSeconds = Mathf.Max(0f, launchMoveSeconds);
            waitingMoveSeconds = Mathf.Max(0f, waitingMoveSeconds);
            flowerFlySeconds = Mathf.Max(0f, flowerFlySeconds);
            potDisappearSeconds = Mathf.Max(0f, potDisappearSeconds);
            flowerFlyArcHeight = Mathf.Max(0f, flowerFlyArcHeight);
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
            DrawLayoutLabels(previewCellSize, previewFlowerScale, previewPotSpacing, previewQueuedPotSpacing);
        }

        private void HandlePointerInput()
        {
            if (_session == null || _session.State != LevelPlayState.Playing)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryClickScreenPosition(Input.mousePosition);
            }

            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    TryClickScreenPosition(touch.position);
                }
            }
        }

        private void TryClickScreenPosition(Vector2 screenPosition)
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                Debug.LogWarning("PrototypeLevelRunner could not find a MainCamera for 2D clicks.");
                return;
            }

            var worldPosition = _camera.ScreenToWorldPoint(screenPosition);
            var hit = Physics2D.OverlapPoint(worldPosition);
            if (hit == null)
            {
                return;
            }

            var target = hit.GetComponent<PrototypeClickTarget>();
            if (target != null)
            {
                target.Click();
            }
        }

        private void TickActivePots()
        {
            var activePots = new List<PotModel>(_session.ActivePots);
            for (var i = 0; i < activePots.Count && _session.State == LevelPlayState.Playing; i++)
            {
                TrackPotentialFlowerTarget(activePots[i]);
                _session.TryCollectAtCurrentConveyorPosition(activePots[i], out _);
            }

            if (_session.State != LevelPlayState.Playing)
            {
                return;
            }

            _session.AdvanceConveyor();
            UpdateConveyorPotViews();
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
                    ApplyVisualSize(view, _runtimeFlowerScale);
                    SetColor(view, ToUnityColor(cell.Color));
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
        }

        private void BuildWaitingSlotViews()
        {
            for (var slotIndex = 0; slotIndex < _session.WaitingSlots.Capacity; slotIndex++)
            {
                var view = CreateView(waitingSlotPrefab, waitingSlotRoot, $"WaitingSlot_{slotIndex}");
                view.transform.position = WaitingSlotPosition(slotIndex);
                ApplyVisualSize(view, waitingSlotVisualSize);
                SetColor(view, Color.gray);
                var capturedSlot = slotIndex;
                BindClick(view, () => RecallWaitingPot(capturedSlot));
                _waitingSlotViews.Add(view);
            }
        }

        private void LaunchColumn(int columnIndex)
        {
            if (_session.State != LevelPlayState.Playing || !_session.Conveyor.CanEnter)
            {
                return;
            }

            var pot = _session.LaunchFromColumn(columnIndex);
            var view = CreateView(potPrefab, conveyorRoot, $"ActivePot_{columnIndex}");
            view.transform.position = LaunchPosition(columnIndex, 0);
            ApplyVisualSize(view, potVisualSize);
            SetColor(view, ToUnityColor(pot.Color));
            _potViews[pot] = view;
            RefreshLaunchColumnView(columnIndex);
            UpdateConveyorPotViews(pot, launchMoveSeconds, launchMoveEase);
        }

        private void RecallWaitingPot(int slotIndex)
        {
            if (_session.State != LevelPlayState.Playing || !_session.Conveyor.CanEnter)
            {
                return;
            }

            if (!_session.WaitingSlots.TryGetPot(slotIndex, out _))
            {
                return;
            }

            var pot = _session.RecallWaitingPot(slotIndex);
            if (!_potViews.TryGetValue(pot, out var view))
            {
                view = CreateView(potPrefab, conveyorRoot, $"RecalledPot_{slotIndex}");
                ApplyVisualSize(view, potVisualSize);
                SetColor(view, ToUnityColor(pot.Color));
                _potViews[pot] = view;
            }

            view.transform.SetParent(conveyorRoot, true);
            UpdateConveyorPotViews(pot, launchMoveSeconds, launchMoveEase);
        }

        private void RefreshLaunchColumnView(int columnIndex)
        {
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
                SetColor(view, ToUnityColor(pot.Color));
                SetSortingOrder(view, 20 - queueIndex);

                if (queueIndex == 0)
                {
                    var capturedColumn = columnIndex;
                    BindClick(view, () => LaunchColumn(capturedColumn));
                }
                else
                {
                    DisableClick(view);
                }

                views.Add(view);
            }

            _launchColumnViews[columnIndex] = views;
        }

        private void UpdateConveyorPotViews()
        {
            UpdateConveyorPotViews(null, GetConveyorMoveSeconds(), Ease.Linear);
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
                AnimatePotMove(view, ConveyorPosition(conveyorPot), duration, ease);
            }
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
                SetColor(potView, ToUnityColor(pot.Color));
                _potViews[pot] = potView;
            }

            potView.transform.SetParent(waitingSlotRoot, true);
            SetSortingOrder(potView, 30);
            BindClick(potView, () => RecallWaitingPot(slotIndex));
            _waitingPotViews[slotIndex] = potView;
            AnimatePotMove(potView, WaitingPotPosition(slotIndex), waitingMoveSeconds, waitingMoveEase);
        }

        private void OnWaitingPotRecalled(LevelSession session, PotModel pot, int slotIndex)
        {
            if (_waitingPotViews.TryGetValue(slotIndex, out var waitingPotView))
            {
                _waitingPotViews.Remove(slotIndex);
                waitingPotView.transform.SetParent(conveyorRoot, true);
            }
        }

        private void OnConveyorPotRemoved(ConveyorBelt conveyor, ConveyorPot conveyorPot)
        {
            if (!conveyorPot.Pot.IsFull)
            {
                return;
            }

            if (_potViews.TryGetValue(conveyorPot.Pot, out var view))
            {
                _potViews.Remove(conveyorPot.Pot);
                _collectedFlowerCounts.Remove(conveyorPot.Pot);
                AnimateDisappear(view, potDisappearSeconds, potDisappearEase);
            }
        }

        private void OnLevelWon(LevelSession session)
        {
            Debug.Log("Prototype level won.");
        }

        private void OnLevelFailed(LevelSession session, LevelFailureReason reason, PotModel failedPot)
        {
            Debug.Log($"Prototype level failed: {reason}.");
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

        private void TrackPotentialFlowerTarget(PotModel pot)
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
            flowerRoot = EnsureRoot(flowerRoot, "PrototypeFlowers");
            conveyorRoot = EnsureRoot(conveyorRoot, "PrototypeConveyor");
            launchColumnRoot = EnsureRoot(launchColumnRoot, "PrototypeLaunchColumns");
            waitingSlotRoot = EnsureRoot(waitingSlotRoot, "PrototypeWaitingSlots");
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
            layoutCellSize = Mathf.Min(cellSize, maxBoardWorldSize / largestGridAxis);
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
            return Mathf.Max(conveyorStepSeconds, conveyorMoveSeconds);
        }

        private void FitCameraToPrototype()
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

            var minX = Mathf.Min(
                flowerOrigin.x - _runtimeCellSize,
                launchOrigin.x - potVisualSize,
                waitingOrigin.x - waitingSlotVisualSize);
            var maxX = Mathf.Max(
                flowerOrigin.x + _session.FlowerGrid.Width * _runtimeCellSize + _runtimeCellSize,
                launchOrigin.x + (_session.LaunchColumns.Count - 1) * _runtimePotSpacing + potVisualSize,
                waitingOrigin.x + (_session.WaitingSlots.Capacity - 1) * _runtimePotSpacing + waitingSlotVisualSize);
            var minY = Mathf.Min(
                launchOrigin.y - Mathf.Max(0, maxQueueCount - 1) * _runtimeQueuedPotVerticalSpacing - potVisualSize,
                waitingOrigin.y - waitingSlotVisualSize);
            var maxY = flowerOrigin.y + _session.FlowerGrid.Height * _runtimeCellSize + _runtimeCellSize;

            var center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, _camera.transform.position.z);
            var worldWidth = maxX - minX + cameraPadding * 2f;
            var worldHeight = maxY - minY + cameraPadding * 2f;

            _camera.transform.position = center;
            _camera.orthographicSize = Mathf.Max(worldHeight * 0.5f, worldWidth / Mathf.Max(_camera.aspect, 0.01f) * 0.5f);
        }

        private void BindClick(GameObject view, Action clicked)
        {
            if (view.GetComponent<Collider2D>() == null)
            {
                view.AddComponent<BoxCollider2D>();
            }

            var target = view.GetComponent<PrototypeClickTarget>();
            if (target == null)
            {
                target = view.AddComponent<PrototypeClickTarget>();
            }

            target.Bind(clicked);
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
            var spriteRenderer = GetSpriteRenderer(view);
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                var spriteSize = spriteRenderer.sprite.bounds.size;
                var largestAxis = Mathf.Max(spriteSize.x, spriteSize.y);
                if (largestAxis > 0f)
                {
                    var scale = targetWorldSize / largestAxis;
                    view.transform.localScale = Vector3.one * scale;
                    return;
                }
            }

            view.transform.localScale = Vector3.one * targetWorldSize;
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
                views[i].transform.DOKill();
                Destroy(views[i]);
            }

            _launchColumnViews.Remove(columnIndex);
        }

        private Vector3 LaunchPosition(int columnIndex, int queueIndex)
        {
            return new Vector3(
                launchOrigin.x + columnIndex * _runtimePotSpacing,
                launchOrigin.y - queueIndex * _runtimeQueuedPotVerticalSpacing,
                queueIndex * 0.01f);
        }

        private Vector3 WaitingSlotPosition(int slotIndex)
        {
            return new Vector3(waitingOrigin.x + slotIndex * _runtimePotSpacing, waitingOrigin.y, 0f);
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
                return new Vector3(waitingOrigin.x + _session.WaitingSlots.Capacity * _runtimePotSpacing, waitingOrigin.y, 0f);
            }

            var xMin = flowerOrigin.x - _runtimeCellSize;
            var xMax = flowerOrigin.x + _session.FlowerGrid.Width * _runtimeCellSize;
            var yMin = flowerOrigin.y - _runtimeCellSize;
            var yMax = flowerOrigin.y + _session.FlowerGrid.Height * _runtimeCellSize;

            return scan.Edge switch
            {
                ConveyorEdge.Left => new Vector3(xMin, flowerOrigin.y + scan.Lane * _runtimeCellSize, 0f),
                ConveyorEdge.Top => new Vector3(flowerOrigin.x + scan.Lane * _runtimeCellSize, yMax, 0f),
                ConveyorEdge.Right => new Vector3(xMax, flowerOrigin.y + scan.Lane * _runtimeCellSize, 0f),
                ConveyorEdge.Bottom => new Vector3(flowerOrigin.x + scan.Lane * _runtimeCellSize, yMin, 0f),
                _ => transform.position
            };
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
            flowerView.transform.localPosition = ToParentLocalOffset(potView.transform, FlowerInPotWorldOffset(count));
            ApplyVisualSizeInsideParent(flowerView, potVisualSize * 0.22f);
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
            if (useBuiltInTestLevel)
            {
                return CreateBuiltInTestLevel();
            }

            if (levelAsset != null && levelAsset.Definition != null)
            {
                return levelAsset.Definition;
            }

            return levelDefinition ?? new LevelDefinition();
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
            var xMin = flowerOrigin.x - previewCellSize;
            var xMax = flowerOrigin.x + width * previewCellSize;
            var yMin = flowerOrigin.y - previewCellSize;
            var yMax = flowerOrigin.y + height * previewCellSize;

            var bottomLeft = new Vector3(xMin, yMin, 0f);
            var topLeft = new Vector3(xMin, yMax, 0f);
            var topRight = new Vector3(xMax, yMax, 0f);
            var bottomRight = new Vector3(xMax, yMin, 0f);

            Gizmos.color = new Color(0.25f, 0.55f, 1f, 0.85f);
            Gizmos.DrawLine(bottomLeft, topLeft);
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
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
                var position = new Vector3(waitingOrigin.x + slotIndex * previewPotSpacing, waitingOrigin.y, 0f);
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
                var last = new Vector3(waitingOrigin.x + (slotCount - 1) * previewPotSpacing, waitingOrigin.y, 0f);
                Gizmos.DrawLine(first, last);
            }
        }

        private void DrawLayoutLabels(float previewCellSize, float previewFlowerScale, float previewPotSpacing, float previewQueuedPotSpacing)
        {
#if UNITY_EDITOR
            Handles.color = Color.white;
            Handles.Label(
                new Vector3(flowerOrigin.x, flowerOrigin.y + previewCellSize, 0f),
                $"Cell {previewCellSize:0.##} / Flower {previewFlowerScale:0.##}");
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
                _ => Color.white
            };
        }

        private static LevelDefinition CreateBuiltInTestLevel()
        {
            const int width = 10;
            const int height = 10;

            var flowers = new List<FlowerCellDefinition>(width * height);
            var counts = new Dictionary<FlowerColor, int>
            {
                { FlowerColor.Red, 0 },
                { FlowerColor.Yellow, 0 },
                { FlowerColor.Blue, 0 }
            };

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var color = ((x + y * width) % 3) switch
                    {
                        0 => FlowerColor.Red,
                        1 => FlowerColor.Yellow,
                        _ => FlowerColor.Blue
                    };

                    flowers.Add(new FlowerCellDefinition(x, y, color));
                    counts[color]++;
                }
            }

            return new LevelDefinition(
                width,
                height,
                flowers,
                new[]
                {
                    new[] { new PotDefinition(FlowerColor.Red, counts[FlowerColor.Red] / 3), new PotDefinition(FlowerColor.Yellow, counts[FlowerColor.Yellow] / 3), new PotDefinition(FlowerColor.Blue, counts[FlowerColor.Blue] / 3) },
                    new[] { new PotDefinition(FlowerColor.Red, counts[FlowerColor.Red] / 3), new PotDefinition(FlowerColor.Yellow, counts[FlowerColor.Yellow] / 3), new PotDefinition(FlowerColor.Blue, counts[FlowerColor.Blue] / 3) },
                    new[] { new PotDefinition(FlowerColor.Red, counts[FlowerColor.Red] - (counts[FlowerColor.Red] / 3) * 2), new PotDefinition(FlowerColor.Yellow, counts[FlowerColor.Yellow] - (counts[FlowerColor.Yellow] / 3) * 2), new PotDefinition(FlowerColor.Blue, counts[FlowerColor.Blue] - (counts[FlowerColor.Blue] / 3) * 2) }
                });
        }
    }
}
