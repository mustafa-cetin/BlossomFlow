using System.Collections.Generic;
using BlossomFlow.Gameplay.Domain;
using BlossomFlow.Gameplay.Domain.Levels;

namespace BlossomFlow.Gameplay.Presentation
{
    public static class BuiltInPrototypeLevelDefinition
    {
        public static LevelDefinition Create()
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
