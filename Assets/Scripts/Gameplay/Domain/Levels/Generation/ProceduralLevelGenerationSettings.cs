using System;

namespace BlossomFlow.Gameplay.Domain.Levels.Generation
{
    [Serializable]
    public sealed class ProceduralLevelGenerationSettings
    {
        public const int MinGridSize = 1;
        public const int MaxGridSize = 40;
        public const int MinFillPercent = 1;
        public const int MaxFillPercent = 100;
        public const int MinColorCount = 1;

        public int Width = 10;
        public int Height = 10;
        public int FillPercent = 100;
        public int ColorCount = 4;
        public int LaunchColumnCount = LevelDefinition.DefaultLaunchColumnCount;
        public int PotsPerColumn = 4;
        public int WaitingSlotCount = LevelDefinition.DefaultWaitingSlotCount;
        public int ConveyorMaxPots = LevelDefinition.DefaultConveyorMaxPots;
        public LevelDifficulty Difficulty = LevelDifficulty.Normal;
        public bool UseSeed;
        public int Seed;
        public bool PreferCapacityMultiplesOfTen;
        public ConveyorPathDefinition ConveyorPath = new("greenhouse");

        public int TotalCellCount => Width * Height;
        public int TargetFlowerCount => Math.Max(ColorCount, TotalCellCount * FillPercent / 100);
        public int TotalPotCount => LaunchColumnCount * PotsPerColumn;

        public void Validate()
        {
            if (Width < MinGridSize || Width > MaxGridSize)
            {
                throw new ArgumentOutOfRangeException(nameof(Width), $"Width must be between {MinGridSize} and {MaxGridSize}.");
            }

            if (Height < MinGridSize || Height > MaxGridSize)
            {
                throw new ArgumentOutOfRangeException(nameof(Height), $"Height must be between {MinGridSize} and {MaxGridSize}.");
            }

            if (FillPercent < MinFillPercent || FillPercent > MaxFillPercent)
            {
                throw new ArgumentOutOfRangeException(nameof(FillPercent), $"Fill percent must be between {MinFillPercent} and {MaxFillPercent}.");
            }

            var availableColors = Enum.GetValues(typeof(FlowerColor)).Length;
            if (ColorCount < MinColorCount || ColorCount > availableColors)
            {
                throw new ArgumentOutOfRangeException(nameof(ColorCount), $"Color count must be between {MinColorCount} and {availableColors}.");
            }

            if (ColorCount > TotalCellCount)
            {
                throw new ArgumentException("Color count cannot be greater than the number of grid cells.");
            }

            if (LaunchColumnCount < LevelDefinition.MinLaunchColumnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(LaunchColumnCount), $"Launch column count must be at least {LevelDefinition.MinLaunchColumnCount}.");
            }

            if (PotsPerColumn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(PotsPerColumn), "Pots per column must be greater than zero.");
            }

            if (TotalPotCount < ColorCount)
            {
                throw new ArgumentException($"Need at least {ColorCount} total pots because the generated grid uses {ColorCount} colors.");
            }

            if (TotalPotCount > TargetFlowerCount)
            {
                throw new ArgumentException($"Cannot create {TotalPotCount} pots from {TargetFlowerCount} flowers because every pot needs at least 1 capacity.");
            }

            if (WaitingSlotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(WaitingSlotCount), "Waiting slot count must be greater than zero.");
            }

            if (ConveyorMaxPots <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ConveyorMaxPots), "Conveyor max pots must be greater than zero.");
            }
        }
    }
}
