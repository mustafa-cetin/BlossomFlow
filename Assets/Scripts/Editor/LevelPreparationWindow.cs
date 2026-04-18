using System;
using System.Collections.Generic;
using System.Linq;
using BlossomFlow.Gameplay.Domain;
using BlossomFlow.Gameplay.Domain.Levels;
using UnityEditor;
using UnityEngine;

namespace BlossomFlow.Editor
{
    public sealed class LevelPreparationWindow : EditorWindow
    {
        private const int MinGridSize = 1;
        private const int MaxGridSize = 20;
        private const float CellSize = 28f;

        private enum CellBrush
        {
            Empty,
            Red,
            Yellow,
            Blue,
            Pink,
            Purple,
            White
        }

        private LevelDefinitionAsset _asset;
        private int _width = 10;
        private int _height = 10;
        private int _waitingSlotCount = LevelDefinition.DefaultWaitingSlotCount;
        private int _conveyorMaxPots = LevelDefinition.DefaultConveyorMaxPots;
        private CellBrush _brush = CellBrush.Red;
        private FlowerColor?[,] _flowers;
        private readonly List<PotDefinition>[] _launchColumns =
        {
            new(),
            new(),
            new()
        };
        private Vector2 _scroll;
        private string _validationMessage = "No level loaded.";
        private MessageType _validationType = MessageType.Info;

        [MenuItem("Tools/Blossom Flow/Level Preparation")]
        public static void Open()
        {
            var window = GetWindow<LevelPreparationWindow>("Level Preparation");
            window.minSize = new Vector2(620f, 420f);
            window.EnsureWorkingGrid();
            window.ValidateWorkingLevel();
        }

        private void OnEnable()
        {
            EnsureWorkingGrid();
            Selection.selectionChanged += OnSelectionChanged;

            if (_asset == null && Selection.activeObject is LevelDefinitionAsset selectedAsset)
            {
                LoadAsset(selectedAsset);
            }
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnGUI()
        {
            EnsureWorkingGrid();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawAssetToolbar();
            EditorGUILayout.Space(8f);
            DrawLevelSettings();
            EditorGUILayout.Space(8f);
            DrawPalette();
            EditorGUILayout.Space(8f);
            DrawGrid();
            EditorGUILayout.Space(12f);
            DrawLaunchColumns();
            EditorGUILayout.Space(12f);
            DrawValidation();
            EditorGUILayout.EndScrollView();
        }

        private void OnSelectionChanged()
        {
            if (Selection.activeObject is LevelDefinitionAsset selectedAsset && selectedAsset != _asset)
            {
                LoadAsset(selectedAsset);
                Repaint();
            }
        }

        private void DrawAssetToolbar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    _asset = (LevelDefinitionAsset)EditorGUILayout.ObjectField(
                        "Level Asset",
                        _asset,
                        typeof(LevelDefinitionAsset),
                        false);

                    if (EditorGUI.EndChangeCheck() && _asset != null)
                    {
                        LoadAsset(_asset);
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("New Blank"))
                    {
                        NewBlankLevel();
                    }

                    using (new EditorGUI.DisabledScope(_asset == null))
                    {
                        if (GUILayout.Button("Reload Asset"))
                        {
                            LoadAsset(_asset);
                        }

                        if (GUILayout.Button("Save Asset"))
                        {
                            SaveToAsset(_asset);
                        }
                    }

                    if (GUILayout.Button("Save As New Asset"))
                    {
                        SaveAsNewAsset();
                    }
                }
            }
        }

        private void DrawLevelSettings()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                var newWidth = EditorGUILayout.IntSlider("Grid Width", _width, MinGridSize, MaxGridSize);
                var newHeight = EditorGUILayout.IntSlider("Grid Height", _height, MinGridSize, MaxGridSize);
                _waitingSlotCount = Mathf.Max(1, EditorGUILayout.IntField("Waiting Slots", _waitingSlotCount));
                _conveyorMaxPots = Mathf.Max(1, EditorGUILayout.IntField("Conveyor Max Pots", _conveyorMaxPots));

                if (EditorGUI.EndChangeCheck())
                {
                    ResizeGrid(newWidth, newHeight);
                    ValidateWorkingLevel();
                }
            }
        }

        private void DrawPalette()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Flower Brush", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    foreach (CellBrush brush in Enum.GetValues(typeof(CellBrush)))
                    {
                        var previousColor = GUI.backgroundColor;
                        GUI.backgroundColor = BrushColor(brush);

                        if (GUILayout.Toggle(_brush == brush, brush.ToString(), EditorStyles.miniButton, GUILayout.Height(24f)))
                        {
                            _brush = brush;
                        }

                        GUI.backgroundColor = previousColor;
                    }
                }
            }
        }

        private void DrawGrid()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Flower Grid", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Paint flower cells with the selected brush. Empty cells are allowed for prototype layouts.", MessageType.None);

                for (var y = _height - 1; y >= 0; y--)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(y.ToString(), GUILayout.Width(22f), GUILayout.Height(CellSize));

                        for (var x = 0; x < _width; x++)
                        {
                            var cell = _flowers[x, y];
                            var previousColor = GUI.backgroundColor;
                            GUI.backgroundColor = cell.HasValue ? FlowerColorToGui(cell.Value) : new Color(0.28f, 0.28f, 0.28f);

                            if (GUILayout.Button(CellLabel(cell), GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
                            {
                                _flowers[x, y] = BrushToFlowerColor(_brush);
                                ValidateWorkingLevel();
                            }

                            GUI.backgroundColor = previousColor;
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(22f);
                    for (var x = 0; x < _width; x++)
                    {
                        GUILayout.Label(x.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(CellSize));
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Clear Grid"))
                    {
                        ClearGrid();
                    }

                    if (GUILayout.Button("Fill Grid With Brush"))
                    {
                        FillGridWithBrush();
                    }

                    if (GUILayout.Button("Build Queues From Flowers"))
                    {
                        BuildQueuesFromFlowers();
                    }
                }
            }
        }

        private void DrawLaunchColumns()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Pot Queues", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Each column is launched front-first from the top row in this list.", MessageType.None);

                using (new EditorGUILayout.HorizontalScope())
                {
                    for (var columnIndex = 0; columnIndex < LevelDefinition.DefaultLaunchColumnCount; columnIndex++)
                    {
                        DrawLaunchColumn(columnIndex);
                    }
                }
            }
        }

        private void DrawLaunchColumn(int columnIndex)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(170f)))
            {
                EditorGUILayout.LabelField($"Column {columnIndex + 1}", EditorStyles.boldLabel);
                var column = _launchColumns[columnIndex];

                for (var potIndex = 0; potIndex < column.Count; potIndex++)
                {
                    var pot = column[potIndex];

                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        pot.Color = (FlowerColor)EditorGUILayout.EnumPopup(pot.Color, GUILayout.Width(72f));
                        pot.Capacity = Mathf.Max(1, EditorGUILayout.IntField(pot.Capacity, GUILayout.Width(44f)));

                        if (GUILayout.Button("Up", EditorStyles.miniButtonLeft, GUILayout.Width(32f)) && potIndex > 0)
                        {
                            (column[potIndex - 1], column[potIndex]) = (column[potIndex], column[potIndex - 1]);
                            ValidateWorkingLevel();
                            GUIUtility.ExitGUI();
                        }

                        if (GUILayout.Button("X", EditorStyles.miniButtonRight, GUILayout.Width(24f)))
                        {
                            column.RemoveAt(potIndex);
                            ValidateWorkingLevel();
                            GUIUtility.ExitGUI();
                        }
                    }

                    column[potIndex] = pot;
                    if (EditorGUI.EndChangeCheck())
                    {
                        ValidateWorkingLevel();
                    }
                }

                if (GUILayout.Button("Add Pot"))
                {
                    column.Add(new PotDefinition(FlowerColor.Red, 1));
                    ValidateWorkingLevel();
                }
            }
        }

        private void DrawValidation()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
                DrawColorTotals();
                EditorGUILayout.HelpBox(_validationMessage, _validationType);

                if (GUILayout.Button("Validate Now"))
                {
                    ValidateWorkingLevel();
                }
            }
        }

        private void DrawColorTotals()
        {
            foreach (FlowerColor color in Enum.GetValues(typeof(FlowerColor)))
            {
                var flowerCount = CountFlowers(color);
                var potCapacity = CountPotCapacity(color);
                var previousColor = GUI.contentColor;
                GUI.contentColor = flowerCount == potCapacity ? Color.white : Color.yellow;
                EditorGUILayout.LabelField(color.ToString(), $"Flowers: {flowerCount} / Pot Capacity: {potCapacity}");
                GUI.contentColor = previousColor;
            }
        }

        private void LoadAsset(LevelDefinitionAsset asset)
        {
            _asset = asset;
            LoadDefinition(asset.Definition);
            ValidateWorkingLevel();
        }

        private void LoadDefinition(LevelDefinition definition)
        {
            _width = Mathf.Clamp(definition.Width, MinGridSize, MaxGridSize);
            _height = Mathf.Clamp(definition.Height, MinGridSize, MaxGridSize);
            _waitingSlotCount = Mathf.Max(1, definition.WaitingSlotCount);
            _conveyorMaxPots = definition.ConveyorMaxPots <= 0
                ? LevelDefinition.DefaultConveyorMaxPots
                : definition.ConveyorMaxPots;
            _flowers = new FlowerColor?[_width, _height];

            foreach (var flower in definition.Flowers ?? Array.Empty<FlowerCellDefinition>())
            {
                if (flower.X >= 0 && flower.X < _width && flower.Y >= 0 && flower.Y < _height)
                {
                    _flowers[flower.X, flower.Y] = flower.Color;
                }
            }

            for (var i = 0; i < _launchColumns.Length; i++)
            {
                _launchColumns[i].Clear();
                if (definition.LaunchColumns != null && i < definition.LaunchColumns.Length && definition.LaunchColumns[i]?.Pots != null)
                {
                    _launchColumns[i].AddRange(definition.LaunchColumns[i].Pots);
                }
            }
        }

        private void NewBlankLevel()
        {
            _asset = null;
            _width = 10;
            _height = 10;
            _waitingSlotCount = LevelDefinition.DefaultWaitingSlotCount;
            _conveyorMaxPots = LevelDefinition.DefaultConveyorMaxPots;
            _flowers = new FlowerColor?[_width, _height];

            foreach (var column in _launchColumns)
            {
                column.Clear();
            }

            ValidateWorkingLevel();
        }

        private void SaveToAsset(LevelDefinitionAsset asset)
        {
            if (asset == null)
            {
                return;
            }

            Undo.RecordObject(asset, "Save Blossom Flow Level");
            asset.SetDefinition(BuildDefinition(PreservedConveyorPath(asset), PreservedBlockers(asset)));
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            ValidateWorkingLevel();
        }

        private void SaveAsNewAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Blossom Flow Level",
                "LevelDefinition",
                "asset",
                "Choose where to create the level asset.");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var asset = CreateInstance<LevelDefinitionAsset>();
            asset.SetDefinition(BuildDefinition(new ConveyorPathDefinition("greenhouse"), Array.Empty<FlowerBlockerDefinition>()));
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            LoadAsset(asset);
        }

        private LevelDefinition BuildDefinition(ConveyorPathDefinition conveyorPath, FlowerBlockerDefinition[] blockers)
        {
            return new LevelDefinition
            {
                Width = _width,
                Height = _height,
                WaitingSlotCount = _waitingSlotCount,
                ConveyorMaxPots = _conveyorMaxPots,
                Flowers = BuildFlowers(),
                LaunchColumns = _launchColumns
                    .Select(column => new PotQueueColumnDefinition(column))
                    .ToArray(),
                ConveyorPath = conveyorPath ?? new ConveyorPathDefinition("greenhouse"),
                Blockers = blockers ?? Array.Empty<FlowerBlockerDefinition>()
            };
        }

        private FlowerCellDefinition[] BuildFlowers()
        {
            var flowers = new List<FlowerCellDefinition>();
            for (var y = 0; y < _height; y++)
            {
                for (var x = 0; x < _width; x++)
                {
                    if (_flowers[x, y].HasValue)
                    {
                        flowers.Add(new FlowerCellDefinition(x, y, _flowers[x, y].Value));
                    }
                }
            }

            return flowers.ToArray();
        }

        private void ValidateWorkingLevel()
        {
            try
            {
                BuildDefinition(PreservedConveyorPath(_asset), PreservedBlockers(_asset)).Validate();
                _validationMessage = "Level is valid. Flower counts exactly match pot capacity by color.";
                _validationType = MessageType.Info;
            }
            catch (Exception exception)
            {
                _validationMessage = exception.Message;
                _validationType = MessageType.Warning;
            }
        }

        private void ResizeGrid(int newWidth, int newHeight)
        {
            newWidth = Mathf.Clamp(newWidth, MinGridSize, MaxGridSize);
            newHeight = Mathf.Clamp(newHeight, MinGridSize, MaxGridSize);

            if (_flowers != null && newWidth == _width && newHeight == _height)
            {
                return;
            }

            var resized = new FlowerColor?[newWidth, newHeight];
            var copyWidth = _flowers == null ? 0 : Mathf.Min(_width, newWidth);
            var copyHeight = _flowers == null ? 0 : Mathf.Min(_height, newHeight);

            for (var y = 0; y < copyHeight; y++)
            {
                for (var x = 0; x < copyWidth; x++)
                {
                    resized[x, y] = _flowers[x, y];
                }
            }

            _width = newWidth;
            _height = newHeight;
            _flowers = resized;
        }

        private void EnsureWorkingGrid()
        {
            if (_flowers == null || _flowers.GetLength(0) != _width || _flowers.GetLength(1) != _height)
            {
                ResizeGrid(_width, _height);
            }
        }

        private void ClearGrid()
        {
            _flowers = new FlowerColor?[_width, _height];
            ValidateWorkingLevel();
        }

        private void FillGridWithBrush()
        {
            var color = BrushToFlowerColor(_brush);
            for (var y = 0; y < _height; y++)
            {
                for (var x = 0; x < _width; x++)
                {
                    _flowers[x, y] = color;
                }
            }

            ValidateWorkingLevel();
        }

        private void BuildQueuesFromFlowers()
        {
            foreach (var column in _launchColumns)
            {
                column.Clear();
            }

            var columnIndex = 0;
            foreach (FlowerColor color in Enum.GetValues(typeof(FlowerColor)))
            {
                var count = CountFlowers(color);
                if (count <= 0)
                {
                    continue;
                }

                _launchColumns[columnIndex].Add(new PotDefinition(color, count));
                columnIndex = (columnIndex + 1) % _launchColumns.Length;
            }

            ValidateWorkingLevel();
        }

        private int CountFlowers(FlowerColor color)
        {
            var count = 0;
            for (var y = 0; y < _height; y++)
            {
                for (var x = 0; x < _width; x++)
                {
                    if (_flowers[x, y] == color)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private int CountPotCapacity(FlowerColor color)
        {
            return _launchColumns.Sum(column => column.Where(pot => pot.Color == color).Sum(pot => pot.Capacity));
        }

        private ConveyorPathDefinition PreservedConveyorPath(LevelDefinitionAsset asset)
        {
            return asset != null ? asset.Definition.ConveyorPath : new ConveyorPathDefinition("greenhouse");
        }

        private FlowerBlockerDefinition[] PreservedBlockers(LevelDefinitionAsset asset)
        {
            return asset != null ? asset.Definition.Blockers : Array.Empty<FlowerBlockerDefinition>();
        }

        private static FlowerColor? BrushToFlowerColor(CellBrush brush)
        {
            return brush switch
            {
                CellBrush.Red => FlowerColor.Red,
                CellBrush.Yellow => FlowerColor.Yellow,
                CellBrush.Blue => FlowerColor.Blue,
                CellBrush.Pink => FlowerColor.Pink,
                CellBrush.Purple => FlowerColor.Purple,
                CellBrush.White => FlowerColor.White,
                _ => null
            };
        }

        private static Color BrushColor(CellBrush brush)
        {
            return brush == CellBrush.Empty ? new Color(0.45f, 0.45f, 0.45f) : FlowerColorToGui(BrushToFlowerColor(brush).Value);
        }

        private static Color FlowerColorToGui(FlowerColor color)
        {
            return color switch
            {
                FlowerColor.Red => new Color(0.95f, 0.22f, 0.22f),
                FlowerColor.Yellow => new Color(1f, 0.86f, 0.2f),
                FlowerColor.Blue => new Color(0.25f, 0.48f, 0.95f),
                FlowerColor.Pink => new Color(1f, 0.46f, 0.68f),
                FlowerColor.Purple => new Color(0.58f, 0.36f, 0.9f),
                FlowerColor.White => new Color(0.9f, 0.9f, 0.9f),
                _ => Color.white
            };
        }

        private static string CellLabel(FlowerColor? color)
        {
            return color.HasValue ? color.Value.ToString().Substring(0, 1) : "-";
        }
    }
}
