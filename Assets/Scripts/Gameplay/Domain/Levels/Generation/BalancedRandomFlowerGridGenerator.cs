using System;
using System.Collections.Generic;
using System.Linq;

namespace BlossomFlow.Gameplay.Domain.Levels.Generation
{
    public sealed class BalancedRandomFlowerGridGenerator : IProceduralFlowerGridGenerator
    {
        public FlowerCellDefinition[] Generate(ProceduralLevelGenerationSettings settings, Random random)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            settings.Validate();

            var positions = BuildPositions(settings.Width, settings.Height);
            ProceduralLevelRandom.Shuffle(positions, random);

            var colors = Enum.GetValues(typeof(FlowerColor))
                .Cast<FlowerColor>()
                .Take(settings.ColorCount)
                .ToList();
            ProceduralLevelRandom.Shuffle(colors, random);

            var flowerCount = settings.TargetFlowerCount;
            var flowers = new List<FlowerCellDefinition>(flowerCount);
            for (var i = 0; i < flowerCount; i++)
            {
                var color = colors[i % colors.Count];
                var position = positions[i];
                flowers.Add(new FlowerCellDefinition(position.X, position.Y, color));
            }

            ProceduralLevelRandom.Shuffle(flowers, random);
            return flowers.ToArray();
        }

        private static List<(int X, int Y)> BuildPositions(int width, int height)
        {
            var positions = new List<(int X, int Y)>(width * height);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    positions.Add((x, y));
                }
            }

            return positions;
        }
    }
}
