using System;
using System.Collections.Generic;
using System.Linq;
using BlossomFlow.Gameplay.Domain;

namespace BlossomFlow.Gameplay.Domain.Levels
{
    [Serializable]
    public sealed class LevelDefinition
    {
        public const int DefaultLaunchColumnCount = 3;
        public const int DefaultWaitingSlotCount = 5;
        public const int DefaultConveyorMaxPots = ConveyorBelt.DefaultMaxPots;

        public LevelDefinition()
        {
            Width = 1;
            Height = 1;
            Flowers = Array.Empty<FlowerCellDefinition>();
            LaunchColumns = Enumerable.Range(0, DefaultLaunchColumnCount)
                .Select(_ => new PotQueueColumnDefinition())
                .ToArray();
            WaitingSlotCount = DefaultWaitingSlotCount;
            ConveyorMaxPots = DefaultConveyorMaxPots;
            ConveyorPath = new ConveyorPathDefinition();
            Blockers = Array.Empty<FlowerBlockerDefinition>();
        }

        public LevelDefinition(
            int width,
            int height,
            IEnumerable<FlowerCellDefinition> flowers,
            IEnumerable<IEnumerable<PotDefinition>> launchColumns,
            int waitingSlotCount = DefaultWaitingSlotCount,
            ConveyorPathDefinition conveyorPath = null,
            IEnumerable<FlowerBlockerDefinition> blockers = null,
            int conveyorMaxPots = DefaultConveyorMaxPots)
            : this(
                width,
                height,
                flowers,
                (launchColumns ?? throw new ArgumentNullException(nameof(launchColumns)))
                    .Select(column => new PotQueueColumnDefinition(column)),
                waitingSlotCount,
                conveyorPath,
                blockers,
                conveyorMaxPots)
        {
        }

        public LevelDefinition(
            int width,
            int height,
            IEnumerable<FlowerCellDefinition> flowers,
            IEnumerable<PotQueueColumnDefinition> launchColumns,
            int waitingSlotCount = DefaultWaitingSlotCount,
            ConveyorPathDefinition conveyorPath = null,
            IEnumerable<FlowerBlockerDefinition> blockers = null,
            int conveyorMaxPots = DefaultConveyorMaxPots)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
            }

            if (waitingSlotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(waitingSlotCount), "Waiting slot count must be greater than zero.");
            }

            if (conveyorMaxPots <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(conveyorMaxPots), "Conveyor max pots must be greater than zero.");
            }

            Width = width;
            Height = height;
            Flowers = (flowers ?? throw new ArgumentNullException(nameof(flowers))).ToArray();
            LaunchColumns = (launchColumns ?? throw new ArgumentNullException(nameof(launchColumns)))
                .Select(column => column ?? throw new ArgumentException("Launch columns cannot contain null entries.", nameof(launchColumns)))
                .ToArray();
            WaitingSlotCount = waitingSlotCount;
            ConveyorMaxPots = conveyorMaxPots;
            ConveyorPath = conveyorPath ?? new ConveyorPathDefinition("greenhouse");
            Blockers = (blockers ?? Enumerable.Empty<FlowerBlockerDefinition>()).ToArray();
            Validate();
        }

        public int Width;
        public int Height;
        public FlowerCellDefinition[] Flowers;
        public PotQueueColumnDefinition[] LaunchColumns;
        public int WaitingSlotCount;
        public int ConveyorMaxPots;
        public ConveyorPathDefinition ConveyorPath;
        public FlowerBlockerDefinition[] Blockers;

        public ConveyorPath CreateConveyorPath()
        {
            return ConveyorPath.CreatePath(Width, Height);
        }

        public void Validate()
        {
            if (Width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Width), "Width must be greater than zero.");
            }

            if (Height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Height), "Height must be greater than zero.");
            }

            if (WaitingSlotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(WaitingSlotCount), "Waiting slot count must be greater than zero.");
            }

            if (ConveyorMaxPots <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ConveyorMaxPots), "Conveyor max pots must be greater than zero.");
            }

            ValidateFlowers();
            ValidateBlockers();
            ValidateLaunchColumns();
            if (ConveyorPath == null)
            {
                ConveyorPath = new ConveyorPathDefinition("greenhouse");
            }

            ConveyorPath.Validate(Width, Height);
            ValidateColorCapacity();
        }

        private void ValidateFlowers()
        {
            if (Flowers == null)
            {
                throw new ArgumentNullException(nameof(Flowers));
            }

            var occupiedPositions = new HashSet<GridPosition>();
            for (var i = 0; i < Flowers.Length; i++)
            {
                var flower = Flowers[i];
                EnsurePositionInBounds(flower.X, flower.Y, $"Flower {i}");

                var position = new GridPosition(flower.X, flower.Y);
                if (!occupiedPositions.Add(position))
                {
                    throw new ArgumentException($"Duplicate flower cell at {position}.");
                }
            }
        }

        private void ValidateBlockers()
        {
            if (Blockers == null)
            {
                Blockers = Array.Empty<FlowerBlockerDefinition>();
            }

            var blockerPositions = new HashSet<GridPosition>();
            for (var i = 0; i < Blockers.Length; i++)
            {
                var blocker = Blockers[i];
                EnsurePositionInBounds(blocker.X, blocker.Y, $"Blocker {i}");

                var position = new GridPosition(blocker.X, blocker.Y);
                if (!blockerPositions.Add(position))
                {
                    throw new ArgumentException($"Duplicate blocker at {position}.");
                }
            }
        }

        private void ValidateLaunchColumns()
        {
            if (LaunchColumns == null)
            {
                throw new ArgumentNullException(nameof(LaunchColumns));
            }

            if (LaunchColumns.Length != DefaultLaunchColumnCount)
            {
                throw new ArgumentException($"Level data must define exactly {DefaultLaunchColumnCount} selectable pot columns.");
            }

            for (var columnIndex = 0; columnIndex < LaunchColumns.Length; columnIndex++)
            {
                var column = LaunchColumns[columnIndex];
                if (column == null)
                {
                    throw new ArgumentException($"Launch column {columnIndex} is null.");
                }

                foreach (var pot in column)
                {
                    if (pot.Capacity <= 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(pot.Capacity), $"Launch column {columnIndex} contains a pot with invalid capacity.");
                    }
                }
            }
        }

        private void ValidateColorCapacity()
        {
            var flowersByColor = Flowers
                .GroupBy(flower => flower.Color)
                .ToDictionary(group => group.Key, group => group.Count());

            var potCapacityByColor = LaunchColumns
                .SelectMany(column => column)
                .GroupBy(pot => pot.Color)
                .ToDictionary(group => group.Key, group => group.Sum(pot => pot.Capacity));

            foreach (var flowerGroup in flowersByColor)
            {
                potCapacityByColor.TryGetValue(flowerGroup.Key, out var availableCapacity);
                if (availableCapacity != flowerGroup.Value)
                {
                    throw new ArgumentException($"Level must have exactly {flowerGroup.Value} {flowerGroup.Key} pot capacity, but has {availableCapacity}.");
                }
            }

            foreach (var potCapacity in potCapacityByColor)
            {
                flowersByColor.TryGetValue(potCapacity.Key, out var flowerCount);
                if (potCapacity.Value != flowerCount)
                {
                    throw new ArgumentException($"Level has {potCapacity.Value} {potCapacity.Key} pot capacity but only {flowerCount} matching flowers.");
                }
            }
        }

        private void EnsurePositionInBounds(int x, int y, string label)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                throw new ArgumentOutOfRangeException(label, $"{label} at ({x}, {y}) is outside a {Width}x{Height} grid.");
            }
        }
    }
}
