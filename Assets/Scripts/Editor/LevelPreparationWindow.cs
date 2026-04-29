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
            White,
            Orange,
            Black,
            Cyan,
            Magenta
        }

        private LevelDefinitionAsset _asset;
        private int _width = 10;
        private int _height = 10;
        private int _waitingSlotCount = LevelDefinition.DefaultWaitingSlotCount;
        private int _conveyorMaxPots = LevelDefinition.DefaultConveyorMaxPots;
        private LevelDifficulty _difficulty = LevelDifficulty.Normal;
        private CellBrush _brush = CellBrush.Red;
        private FlowerColor?[,] _flowers;
        private string _importText = DefaultImportText;
        private string _importMessage = "Paste level text, then import it into the working level.";
        private MessageType _importMessageType = MessageType.Info;
        private int _randomQueuePotsPerColumn = 4;
        private bool _roundRandomQueueCountsToTens;
        private bool _useRandomQueueSeed;
        private int _randomQueueSeed;
        private string _randomQueueMessage = "Build random queues from the current flower grid.";
        private MessageType _randomQueueMessageType = MessageType.Info;
        private readonly List<PotDefinition>[] _launchColumns =
        {
            new(),
            new(),
            new()
        };
        private Vector2 _scroll;
        private string _validationMessage = "No level loaded.";
        private MessageType _validationType = MessageType.Info;

        private const string DefaultImportText =
            "size 5x3\n" +
            "waiting 5\n" +
            "conveyor 6\n" +
            "difficulty normal\n" +
            "\n" +
            "grid:\n" +
            "R R Y B B\n" +
            "R . Y B B\n" +
            "P P U U W\n" +
            "\n" +
            "queues:\n" +
            "1: R2, Y2\n" +
            "2: B4, P2\n" +
            "3: U2, W1";

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
            DrawTextImport();
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
                _difficulty = (LevelDifficulty)EditorGUILayout.EnumPopup("Difficulty", _difficulty);

                if (EditorGUI.EndChangeCheck())
                {
                    ResizeGrid(newWidth, newHeight);
                    ValidateWorkingLevel();
                }
            }
        }

        private void DrawTextImport()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Text Import", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Format: size 5x3, waiting 5, conveyor 6, difficulty normal|hard|very-hard, grid:, queues:. Colors: R Red, Y Yellow, B Blue, P Pink, U Purple, W White, . Empty.",
                    MessageType.None);

                _importText = EditorGUILayout.TextArea(_importText, GUILayout.MinHeight(130f));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Import Text"))
                    {
                        ImportTextLevel(false);
                    }

                    if (GUILayout.Button("Import Text + Auto Queues"))
                    {
                        ImportTextLevel(true);
                    }

                    if (GUILayout.Button("Copy Current Level As Text"))
                    {
                        _importText = ExportWorkingLevelText();
                        _importMessage = "Current working level copied into the import box.";
                        _importMessageType = MessageType.Info;
                    }
                }

                EditorGUILayout.HelpBox(_importMessage, _importMessageType);
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

                EditorGUILayout.Space(8f);
                DrawRandomQueueBuilder();
            }
        }

        private void DrawRandomQueueBuilder()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Random Queue Builder", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Creates random pot queues from the current grid while keeping every color capacity equal to its flower count.",
                    MessageType.None);

                _randomQueuePotsPerColumn = Mathf.Max(1, EditorGUILayout.IntField("Pots Per Queue", _randomQueuePotsPerColumn));
                _roundRandomQueueCountsToTens = EditorGUILayout.Toggle("Counts End In 0", _roundRandomQueueCountsToTens);
                _useRandomQueueSeed = EditorGUILayout.Toggle("Use Seed", _useRandomQueueSeed);

                using (new EditorGUI.DisabledScope(!_useRandomQueueSeed))
                {
                    _randomQueueSeed = EditorGUILayout.IntField("Seed", _randomQueueSeed);
                }

                if (GUILayout.Button("Build Random Queues From Grid"))
                {
                    BuildRandomQueuesFromFlowers();
                }

                EditorGUILayout.HelpBox(_randomQueueMessage, _randomQueueMessageType);
            }
        }

        private void DrawLaunchColumns()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Pot Queues", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Each column is launched front-first from the top row in this list. Link id 0 is a normal pot; matching positive link ids create a Linked Pot group across adjacent columns.", MessageType.None);

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
                        pot.LinkedPairId = Mathf.Max(0, EditorGUILayout.IntField(pot.LinkedPairId, GUILayout.Width(36f)));

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
            _difficulty = definition.Difficulty;
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
            _difficulty = LevelDifficulty.Normal;
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
                Difficulty = _difficulty,
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

        private void ImportTextLevel(bool forceAutoQueues)
        {
            try
            {
                var imported = ParseImportText(_importText);
                _width = imported.Width;
                _height = imported.Height;
                _waitingSlotCount = imported.WaitingSlotCount;
                _conveyorMaxPots = imported.ConveyorMaxPots;
                _difficulty = imported.Difficulty;
                _flowers = imported.Flowers;

                foreach (var column in _launchColumns)
                {
                    column.Clear();
                }

                if (forceAutoQueues || !imported.HasQueues)
                {
                    BuildQueuesFromFlowers();
                }
                else
                {
                    for (var i = 0; i < _launchColumns.Length; i++)
                    {
                        _launchColumns[i].AddRange(imported.LaunchColumns[i]);
                    }

                    ValidateWorkingLevel();
                }

                _importMessage = forceAutoQueues
                    ? "Imported grid and rebuilt queues from flower counts."
                    : imported.HasQueues
                        ? "Imported grid, settings, and queues."
                        : "Imported grid and settings. Queues were rebuilt because no queues section was found.";
                _importMessageType = MessageType.Info;
            }
            catch (Exception exception)
            {
                _importMessage = exception.Message;
                _importMessageType = MessageType.Error;
            }
        }

        private ImportedLevel ParseImportText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Import text is empty.");
            }

            int? declaredWidth = null;
            int? declaredHeight = null;
            var waitingSlotCount = _waitingSlotCount;
            var conveyorMaxPots = _conveyorMaxPots;
            var difficulty = _difficulty;
            var gridRows = new List<List<FlowerColor?>>();
            var launchColumns = Enumerable.Range(0, LevelDefinition.DefaultLaunchColumnCount)
                .Select(_ => new List<PotDefinition>())
                .ToArray();
            var hasQueues = false;
            var section = ImportSection.Header;

            var rawLines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (var lineIndex = 0; lineIndex < rawLines.Length; lineIndex++)
            {
                var line = StripImportComment(rawLines[lineIndex]).Trim();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (IsSectionHeader(line, "grid"))
                {
                    section = ImportSection.Grid;
                    continue;
                }

                if (IsSectionHeader(line, "queues"))
                {
                    section = ImportSection.Queues;
                    hasQueues = true;
                    continue;
                }

                if (section == ImportSection.Grid)
                {
                    gridRows.Add(ParseGridRow(line, lineIndex + 1));
                    continue;
                }

                if (section == ImportSection.Queues)
                {
                    ParseQueueLine(line, lineIndex + 1, launchColumns);
                    continue;
                }

                ParseImportSetting(line, lineIndex + 1, ref declaredWidth, ref declaredHeight, ref waitingSlotCount, ref conveyorMaxPots, ref difficulty);
            }

            if (gridRows.Count == 0)
            {
                throw new ArgumentException("Import text must include a grid: section with at least one row.");
            }

            var width = declaredWidth ?? gridRows.Max(row => row.Count);
            var height = declaredHeight ?? gridRows.Count;
            width = ValidateImportSize(width, "width");
            height = ValidateImportSize(height, "height");

            if (gridRows.Count > height)
            {
                throw new ArgumentException($"Grid has {gridRows.Count} rows, but declared height is {height}.");
            }

            var flowers = new FlowerColor?[width, height];
            for (var rowIndex = 0; rowIndex < gridRows.Count; rowIndex++)
            {
                var row = gridRows[rowIndex];
                if (row.Count > width)
                {
                    throw new ArgumentException($"Grid row {rowIndex + 1} has {row.Count} cells, but width is {width}.");
                }

                var y = height - 1 - rowIndex;
                for (var x = 0; x < row.Count; x++)
                {
                    flowers[x, y] = row[x];
                }
            }

            waitingSlotCount = Mathf.Max(1, waitingSlotCount);
            conveyorMaxPots = Mathf.Max(1, conveyorMaxPots);

            return new ImportedLevel(
                width,
                height,
                waitingSlotCount,
                conveyorMaxPots,
                difficulty,
                flowers,
                launchColumns,
                hasQueues);
        }

        private string ExportWorkingLevelText()
        {
            var lines = new List<string>
            {
                $"size {_width}x{_height}",
                $"waiting {_waitingSlotCount}",
                $"conveyor {_conveyorMaxPots}",
                $"difficulty {FormatDifficultyImportToken(_difficulty)}",
                string.Empty,
                "grid:"
            };

            for (var y = _height - 1; y >= 0; y--)
            {
                var row = new List<string>(_width);
                for (var x = 0; x < _width; x++)
                {
                    row.Add(FlowerColorToImportToken(_flowers[x, y]));
                }

                lines.Add(string.Join(" ", row));
            }

            lines.Add(string.Empty);
            lines.Add("queues:");

            for (var i = 0; i < _launchColumns.Length; i++)
            {
                var pots = _launchColumns[i]
                    .Select(FormatPotImportToken);
                lines.Add($"{i + 1}: {string.Join(", ", pots)}");
            }

            return string.Join("\n", lines);
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

        private void BuildRandomQueuesFromFlowers()
        {
            try
            {
                var counts = GetFlowerCounts()
                    .Where(pair => pair.Value > 0)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                if (counts.Count == 0)
                {
                    throw new ArgumentException("Grid has no flowers. Paint or import flowers before building queues.");
                }

                var totalPots = _randomQueuePotsPerColumn * _launchColumns.Length;
                var totalFlowers = counts.Sum(pair => pair.Value);

                if (totalPots < counts.Count)
                {
                    throw new ArgumentException($"Need at least {counts.Count} total pots because the grid uses {counts.Count} colors. Increase Pots Per Queue.");
                }

                if (totalPots > totalFlowers)
                {
                    throw new ArgumentException($"Cannot create {totalPots} pots from {totalFlowers} flowers because every pot needs at least 1 capacity.");
                }

                var random = _useRandomQueueSeed ? new System.Random(_randomQueueSeed) : new System.Random();
                var roundingMessage = string.Empty;
                Dictionary<FlowerColor, int> potColorCounts;
                List<PotDefinition> pots;

                if (_roundRandomQueueCountsToTens)
                {
                    try
                    {
                        potColorCounts = AllocateRandomPotCountsByColor(counts, totalPots, random, 10);
                        pots = BuildRandomPots(counts, potColorCounts, random, 10);
                        roundingMessage = " Pot counts were rounded to 10.";
                    }
                    catch (ArgumentException exception)
                    {
                        random = _useRandomQueueSeed ? new System.Random(_randomQueueSeed) : random;
                        potColorCounts = AllocateRandomPotCountsByColor(counts, totalPots, random);
                        pots = BuildRandomPots(counts, potColorCounts, random);
                        roundingMessage = $" Rounded counts were not possible ({exception.Message}); built normal random counts.";
                    }
                }
                else
                {
                    potColorCounts = AllocateRandomPotCountsByColor(counts, totalPots, random);
                    pots = BuildRandomPots(counts, potColorCounts, random);
                }

                Shuffle(pots, random);

                foreach (var column in _launchColumns)
                {
                    column.Clear();
                }

                var potIndex = 0;
                for (var columnIndex = 0; columnIndex < _launchColumns.Length; columnIndex++)
                {
                    for (var i = 0; i < _randomQueuePotsPerColumn; i++)
                    {
                        _launchColumns[columnIndex].Add(pots[potIndex]);
                        potIndex++;
                    }
                }

                ValidateWorkingLevel();
                _randomQueueMessage = $"Built {_launchColumns.Count} queues with {_randomQueuePotsPerColumn} pots each.{roundingMessage}";
                _randomQueueMessageType = MessageType.Info;
            }
            catch (Exception exception)
            {
                _randomQueueMessage = exception.Message;
                _randomQueueMessageType = MessageType.Error;
            }
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

        private Dictionary<FlowerColor, int> GetFlowerCounts()
        {
            var counts = new Dictionary<FlowerColor, int>();
            foreach (FlowerColor color in Enum.GetValues(typeof(FlowerColor)))
            {
                counts[color] = CountFlowers(color);
            }

            return counts;
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

        private static Dictionary<FlowerColor, int> AllocateRandomPotCountsByColor(
            IReadOnlyDictionary<FlowerColor, int> flowerCounts,
            int totalPots,
            System.Random random,
            int minimumCapacityPerPot = 1)
        {
            if (minimumCapacityPerPot <= 0)
            {
                throw new ArgumentException("Minimum capacity per pot must be positive.");
            }

            foreach (var pair in flowerCounts)
            {
                if (pair.Value < minimumCapacityPerPot || pair.Value % minimumCapacityPerPot != 0)
                {
                    throw new ArgumentException($"each used color needs a flower count that is a multiple of {minimumCapacityPerPot}");
                }
            }

            var potCounts = flowerCounts.ToDictionary(pair => pair.Key, _ => 1);
            var remainingPots = totalPots - potCounts.Count;

            while (remainingPots > 0)
            {
                var eligibleColors = flowerCounts
                    .Where(pair => (potCounts[pair.Key] + 1) * minimumCapacityPerPot <= pair.Value)
                    .Select(pair => pair.Key)
                    .ToList();

                if (eligibleColors.Count == 0)
                {
                    throw new ArgumentException("Could not allocate random pots for the current flower counts.");
                }

                var color = PickWeightedColor(eligibleColors, flowerCounts, random);
                potCounts[color]++;
                remainingPots--;
            }

            return potCounts;
        }

        private static List<PotDefinition> BuildRandomPots(
            IReadOnlyDictionary<FlowerColor, int> flowerCounts,
            IReadOnlyDictionary<FlowerColor, int> potColorCounts,
            System.Random random,
            int capacityUnit = 1)
        {
            var pots = new List<PotDefinition>();
            foreach (var pair in flowerCounts)
            {
                var capacities = SplitCapacityRandomly(pair.Value, potColorCounts[pair.Key], random, capacityUnit);
                pots.AddRange(capacities.Select(capacity => new PotDefinition(pair.Key, capacity)));
            }

            return pots;
        }

        private static List<int> SplitCapacityRandomly(
            int totalCapacity,
            int potCount,
            System.Random random,
            int capacityUnit = 1)
        {
            if (capacityUnit <= 0)
            {
                throw new ArgumentException("Capacity unit must be positive.");
            }

            if (potCount <= 0 || totalCapacity < potCount * capacityUnit || totalCapacity % capacityUnit != 0)
            {
                throw new ArgumentException("Invalid pot split requested.");
            }

            var capacities = Enumerable.Repeat(capacityUnit, potCount).ToList();
            var remainingCapacity = totalCapacity - (potCount * capacityUnit);

            while (remainingCapacity > 0)
            {
                capacities[random.Next(capacities.Count)] += capacityUnit;
                remainingCapacity -= capacityUnit;
            }

            Shuffle(capacities, random);
            return capacities;
        }

        private static FlowerColor PickWeightedColor(
            IReadOnlyList<FlowerColor> colors,
            IReadOnlyDictionary<FlowerColor, int> weights,
            System.Random random)
        {
            var totalWeight = colors.Sum(color => weights[color]);
            var roll = random.Next(totalWeight);

            foreach (var color in colors)
            {
                roll -= weights[color];
                if (roll < 0)
                {
                    return color;
                }
            }

            return colors[colors.Count - 1];
        }

        private static void Shuffle<T>(IList<T> values, System.Random random)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var swapIndex = random.Next(i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
            }
        }

        private static void ParseImportSetting(
            string line,
            int lineNumber,
            ref int? declaredWidth,
            ref int? declaredHeight,
            ref int waitingSlotCount,
            ref int conveyorMaxPots,
            ref LevelDifficulty difficulty)
        {
            var parts = SplitSettingLine(line);
            if (parts.Length < 2)
            {
                throw new ArgumentException($"Line {lineNumber}: expected a setting such as size 5x3, waiting 5, or conveyor 6.");
            }

            var key = parts[0].ToLowerInvariant();
            var value = parts[1];

            switch (key)
            {
                case "size":
                    ParseSize(value, lineNumber, out declaredWidth, out declaredHeight);
                    break;
                case "waiting":
                case "waitingslots":
                case "waiting-slots":
                    waitingSlotCount = ParsePositiveInt(value, lineNumber, key);
                    break;
                case "conveyor":
                case "conveyormaxpots":
                case "conveyor-max-pots":
                    conveyorMaxPots = ParsePositiveInt(value, lineNumber, key);
                    break;
                case "difficulty":
                case "leveldifficulty":
                case "level-difficulty":
                    difficulty = ParseDifficulty(value, lineNumber);
                    break;
                default:
                    throw new ArgumentException($"Line {lineNumber}: unknown setting '{parts[0]}'.");
            }
        }

        private static LevelDifficulty ParseDifficulty(string value, int lineNumber)
        {
            var normalized = value.Replace("-", string.Empty).Replace("_", string.Empty);
            if (Enum.TryParse(normalized, true, out LevelDifficulty difficulty))
            {
                return difficulty;
            }

            throw new ArgumentException($"Line {lineNumber}: difficulty must be normal, hard, or very-hard.");
        }

        private static string[] SplitSettingLine(string line)
        {
            return line
                .Replace(":", " ")
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static void ParseSize(string value, int lineNumber, out int? width, out int? height)
        {
            var sizeParts = value.ToLowerInvariant().Split('x');
            if (sizeParts.Length != 2 ||
                !int.TryParse(sizeParts[0], out var parsedWidth) ||
                !int.TryParse(sizeParts[1], out var parsedHeight))
            {
                throw new ArgumentException($"Line {lineNumber}: size must look like 5x3.");
            }

            width = parsedWidth;
            height = parsedHeight;
        }

        private static int ParsePositiveInt(string value, int lineNumber, string label)
        {
            if (!int.TryParse(value, out var result) || result <= 0)
            {
                throw new ArgumentException($"Line {lineNumber}: {label} must be a positive number.");
            }

            return result;
        }

        private static List<FlowerColor?> ParseGridRow(string line, int lineNumber)
        {
            var tokens = TokenizeGridRow(line);
            if (tokens.Length == 0)
            {
                throw new ArgumentException($"Line {lineNumber}: grid row is empty.");
            }

            var row = new List<FlowerColor?>(tokens.Length);
            foreach (var token in tokens)
            {
                row.Add(ParseFlowerToken(token, lineNumber));
            }

            return row;
        }

        private static string[] TokenizeGridRow(string line)
        {
            var hasSeparators = line.Any(char.IsWhiteSpace) || line.Contains(",") || line.Contains(";");
            if (hasSeparators)
            {
                return line
                    .Split(new[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            }

            return line.Select(character => character.ToString()).ToArray();
        }

        private static void ParseQueueLine(string line, int lineNumber, List<PotDefinition>[] launchColumns)
        {
            var separatorIndex = line.IndexOf(':');
            if (separatorIndex < 0)
            {
                throw new ArgumentException($"Line {lineNumber}: queue line must look like 1: R2, Y2.");
            }

            var columnLabel = line.Substring(0, separatorIndex).Trim();
            var columnIndex = ParseQueueColumnIndex(columnLabel, lineNumber);
            var potText = line.Substring(separatorIndex + 1).Trim();
            launchColumns[columnIndex].Clear();

            if (string.IsNullOrEmpty(potText))
            {
                return;
            }

            var potTokens = potText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var potToken in potTokens)
            {
                launchColumns[columnIndex].Add(ParsePotToken(potToken.Trim(), lineNumber));
            }
        }

        private static int ParseQueueColumnIndex(string columnLabel, int lineNumber)
        {
            var digits = new string(columnLabel.Where(char.IsDigit).ToArray());
            if (!int.TryParse(digits, out var columnNumber) ||
                columnNumber < 1 ||
                columnNumber > LevelDefinition.DefaultLaunchColumnCount)
            {
                throw new ArgumentException($"Line {lineNumber}: queue column must be between 1 and {LevelDefinition.DefaultLaunchColumnCount}.");
            }

            return columnNumber - 1;
        }

        private static PotDefinition ParsePotToken(string token, int lineNumber)
        {
            var normalized = token.Replace(":", " ").Trim();
            var split = normalized.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length == 2)
            {
                ParseCapacityAndLink(split[1], lineNumber, out var capacity, out var linkedPairId);
                return new PotDefinition(
                    ParseRequiredFlowerColor(split[0], lineNumber),
                    capacity,
                    linkedPairId);
            }

            var compact = normalized.Replace(" ", string.Empty);
            var firstDigitIndex = compact.IndexOfAny("0123456789".ToCharArray());
            if (firstDigitIndex <= 0 || firstDigitIndex >= compact.Length)
            {
                throw new ArgumentException($"Line {lineNumber}: pot token '{token}' must look like R2 or Red 2.");
            }

            var colorToken = compact.Substring(0, firstDigitIndex);
            var capacityToken = compact.Substring(firstDigitIndex);
            ParseCapacityAndLink(capacityToken, lineNumber, out var compactCapacity, out var compactLinkedPairId);
            return new PotDefinition(
                ParseRequiredFlowerColor(colorToken, lineNumber),
                compactCapacity,
                compactLinkedPairId);
        }

        private static string FormatPotImportToken(PotDefinition pot)
        {
            var token = $"{FlowerColorToImportToken(pot.Color)}{pot.Capacity}";
            return pot.LinkedPairId > 0 ? $"{token}~{pot.LinkedPairId}" : token;
        }

        private static string FormatDifficultyImportToken(LevelDifficulty difficulty)
        {
            return difficulty == LevelDifficulty.VeryHard ? "very-hard" : difficulty.ToString().ToLowerInvariant();
        }

        private static void ParseCapacityAndLink(string token, int lineNumber, out int capacity, out int linkedPairId)
        {
            var linkSeparatorIndex = token.IndexOf('~');
            if (linkSeparatorIndex < 0)
            {
                capacity = ParsePositiveInt(token, lineNumber, "pot capacity");
                linkedPairId = 0;
                return;
            }

            capacity = ParsePositiveInt(token.Substring(0, linkSeparatorIndex), lineNumber, "pot capacity");
            linkedPairId = ParsePositiveInt(token.Substring(linkSeparatorIndex + 1), lineNumber, "linked group id");
        }

        private static FlowerColor? ParseFlowerToken(string token, int lineNumber)
        {
            var normalized = token.Trim().ToLowerInvariant();
            if (normalized == "." || normalized == "-" || normalized == "_" || normalized == "empty" || normalized == "e")
            {
                return null;
            }

            return ParseRequiredFlowerColor(token, lineNumber);
        }

        private static FlowerColor ParseRequiredFlowerColor(string token, int lineNumber)
        {
            var normalized = token.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "r":
                case "red":
                    return FlowerColor.Red;
                case "y":
                case "yellow":
                    return FlowerColor.Yellow;
                case "b":
                case "blue":
                    return FlowerColor.Blue;
                case "p":
                case "pink":
                    return FlowerColor.Pink;
                case "u":
                case "purple":
                    return FlowerColor.Purple;
                case "w":
                case "white":
                    return FlowerColor.White;
                case "o":
                case "orange":
                    return FlowerColor.Orange;
                case "k":
                case "black":
                    return FlowerColor.Black;
                case "c":
                case "cyan":
                    return FlowerColor.Cyan;
                case "m":
                case "magenta":
                    return FlowerColor.Magenta;
                default:
                    throw new ArgumentException($"Line {lineNumber}: unknown color token '{token}'.");
            }
        }

        private static int ValidateImportSize(int value, string label)
        {
            if (value < MinGridSize || value > MaxGridSize)
            {
                throw new ArgumentException($"Imported grid {label} must be between {MinGridSize} and {MaxGridSize}.");
            }

            return value;
        }

        private static bool IsSectionHeader(string line, string sectionName)
        {
            return string.Equals(line.Trim().TrimEnd(':'), sectionName, StringComparison.OrdinalIgnoreCase);
        }

        private static string StripImportComment(string line)
        {
            var hashIndex = line.IndexOf('#');
            var slashIndex = line.IndexOf("//", StringComparison.Ordinal);
            var commentIndex = -1;

            if (hashIndex >= 0)
            {
                commentIndex = hashIndex;
            }

            if (slashIndex >= 0 && (commentIndex < 0 || slashIndex < commentIndex))
            {
                commentIndex = slashIndex;
            }

            return commentIndex >= 0 ? line.Substring(0, commentIndex) : line;
        }

        private static string FlowerColorToImportToken(FlowerColor? color)
        {
            return color.HasValue ? FlowerColorToImportToken(color.Value) : ".";
        }

        private static string FlowerColorToImportToken(FlowerColor color)
        {
            return color switch
            {
                FlowerColor.Red => "R",
                FlowerColor.Yellow => "Y",
                FlowerColor.Blue => "B",
                FlowerColor.Pink => "P",
                FlowerColor.Purple => "U",
                FlowerColor.White => "W",
                FlowerColor.Orange => "O",
                FlowerColor.Black => "K",
                FlowerColor.Cyan => "C",
                FlowerColor.Magenta => "M",
                _ => "."
            };
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
                CellBrush.Orange => FlowerColor.Orange,
                CellBrush.Black => FlowerColor.Black,
                CellBrush.Cyan => FlowerColor.Cyan,
                CellBrush.Magenta => FlowerColor.Magenta,
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
                FlowerColor.Orange => new Color(1f, 0.55f, 0.12f),
                FlowerColor.Black => new Color(0.08f, 0.08f, 0.08f),
                FlowerColor.Cyan => new Color(0.1f, 0.8f, 0.9f),
                FlowerColor.Magenta => new Color(0.9f, 0.15f, 0.85f),
                _ => Color.white
            };
        }

        private static string CellLabel(FlowerColor? color)
        {
            return color.HasValue ? FlowerColorToImportToken(color.Value) : "-";
        }

        private enum ImportSection
        {
            Header,
            Grid,
            Queues
        }

        private readonly struct ImportedLevel
        {
            public ImportedLevel(
                int width,
                int height,
                int waitingSlotCount,
                int conveyorMaxPots,
                LevelDifficulty difficulty,
                FlowerColor?[,] flowers,
                List<PotDefinition>[] launchColumns,
                bool hasQueues)
            {
                Width = width;
                Height = height;
                WaitingSlotCount = waitingSlotCount;
                ConveyorMaxPots = conveyorMaxPots;
                Difficulty = difficulty;
                Flowers = flowers;
                LaunchColumns = launchColumns;
                HasQueues = hasQueues;
            }

            public int Width { get; }
            public int Height { get; }
            public int WaitingSlotCount { get; }
            public int ConveyorMaxPots { get; }
            public LevelDifficulty Difficulty { get; }
            public FlowerColor?[,] Flowers { get; }
            public List<PotDefinition>[] LaunchColumns { get; }
            public bool HasQueues { get; }
        }
    }
}
