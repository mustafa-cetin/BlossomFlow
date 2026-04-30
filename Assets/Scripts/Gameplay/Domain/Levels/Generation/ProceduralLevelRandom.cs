using System;
using System.Collections.Generic;
using System.Linq;

namespace BlossomFlow.Gameplay.Domain.Levels.Generation
{
    internal static class ProceduralLevelRandom
    {
        public static void Shuffle<T>(IList<T> values, Random random)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var swapIndex = random.Next(i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
            }
        }

        public static FlowerColor PickWeightedColor(
            IReadOnlyList<FlowerColor> colors,
            IReadOnlyDictionary<FlowerColor, int> weights,
            Random random)
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
    }
}
