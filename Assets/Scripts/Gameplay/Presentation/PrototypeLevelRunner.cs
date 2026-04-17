using System;
using System.Collections.Generic;
using BlossomFlow.Gameplay.Domain;
using BlossomFlow.Gameplay.Domain.Levels;
using Reflex.Attributes;
using UnityEngine;

namespace BlossomFlow.Gameplay.Presentation
{
    public sealed class PrototypeLevelRunner : MonoBehaviour
    {
        [Header("Level")]
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
        [SerializeField] private float conveyorStepSeconds = 0.45f;
        [SerializeField] private bool autoFitCamera = true;
        [SerializeField] private float cameraPadding = 0.6f;

        private readonly Dictionary<FlowerCell, GameObject> _flowerViews = new();
        private readonly Dictionary<PotModel, GameObject> _potViews = new();
        private readonly Dictionary<int, GameObject> _waitingPotViews = new();
        private readonly Dictionary<int, List<GameObject>> _launchColumnViews = new();
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

            var definition = useBuiltInTestLevel || levelDefinition == null
                ? CreateBuiltInTestLevel()
                : levelDefinition;

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
            ApplyVisualSize(view, potVisualSize);
            SetColor(view, ToUnityColor(pot.Color));
            _potViews[pot] = view;
            RefreshLaunchColumnView(columnIndex);
            UpdateConveyorPotViews();
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
            UpdateConveyorPotViews();
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
                ApplyVisualSize(view, queueIndex == 0 ? potVisualSize : potVisualSize * 0.82f);
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
            foreach (var conveyorPot in _session.Conveyor.Pots)
            {
                if (!_potViews.TryGetValue(conveyorPot.Pot, out var view))
                {
                    continue;
                }

                view.transform.position = ConveyorPosition(conveyorPot);
            }
        }

        private void OnFlowerRemoved(FlowerGrid grid, FlowerCell cell)
        {
            if (_flowerViews.TryGetValue(cell, out var view))
            {
                Destroy(view);
                _flowerViews.Remove(cell);
            }
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
            potView.transform.position = WaitingPotPosition(slotIndex);
            SetSortingOrder(potView, 30);
            BindClick(potView, () => RecallWaitingPot(slotIndex));
            _waitingPotViews[slotIndex] = potView;
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
                Destroy(view);
                _potViews.Remove(conveyorPot.Pot);
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
            var largestGridAxis = Mathf.Max(_session.FlowerGrid.Width, _session.FlowerGrid.Height);
            _runtimeCellSize = Mathf.Min(cellSize, maxBoardWorldSize / largestGridAxis);
            _runtimeFlowerScale = _runtimeCellSize * flowerScaleMultiplier;
            _runtimePotSpacing = Mathf.Max(potSpacing, potVisualSize * 1.6f);
            _runtimeQueuedPotVerticalSpacing = Mathf.Max(queuedPotVerticalSpacing, potVisualSize * 1.35f);
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
