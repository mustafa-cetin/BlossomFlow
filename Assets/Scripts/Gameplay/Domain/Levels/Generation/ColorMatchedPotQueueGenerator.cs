using System;
using System.Collections.Generic;
using System.Linq;

namespace BlossomFlow.Gameplay.Domain.Levels.Generation
{
    public sealed class ColorMatchedPotQueueGenerator : IProceduralPotQueueGenerator
    {
        public PotQueueColumnDefinition[] Generate(
            FlowerCellDefinition[] flowers,
            ProceduralLevelGenerationSettings settings,
            Random random)
        {
            if (flowers == null)
            {
                throw new ArgumentNullException(nameof(flowers));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            settings.Validate();

            var flowerCounts = flowers
                .GroupBy(flower => flower.Color)
                .ToDictionary(group => group.Key, group => group.Count());

            if (flowerCounts.Count == 0)
            {
                throw new ArgumentException("Generated grid has no flowers.", nameof(flowers));
            }

            var totalPots = settings.TotalPotCount;
            var potColorCounts = AllocatePotCountsByColor(
                flowerCounts,
                totalPots,
                random,
                settings.PreferCapacityMultiplesOfTen ? 10 : 1,
                out var usedFallback);

            var capacityUnit = settings.PreferCapacityMultiplesOfTen && !usedFallback ? 10 : 1;
            var pots = BuildPots(flowerCounts, potColorCounts, random, capacityUnit);
            ProceduralLevelRandom.Shuffle(pots, random);

            return SplitIntoColumns(pots, settings.LaunchColumnCount, settings.PotsPerColumn);
        }

        private static Dictionary<FlowerColor, int> AllocatePotCountsByColor(
            IReadOnlyDictionary<FlowerColor, int> flowerCounts,
            int totalPots,
            Random random,
            int minimumCapacityPerPot,
            out bool usedFallback)
        {
            usedFallback = false;
            try
            {
                return AllocatePotCountsByColor(flowerCounts, totalPots, random, minimumCapacityPerPot);
            }
            catch (ArgumentException)
            {
                if (minimumCapacityPerPot == 1)
                {
                    throw;
                }

                usedFallback = true;
                return AllocatePotCountsByColor(flowerCounts, totalPots, random, 1);
            }
        }

        private static Dictionary<FlowerColor, int> AllocatePotCountsByColor(
            IReadOnlyDictionary<FlowerColor, int> flowerCounts,
            int totalPots,
            Random random,
            int minimumCapacityPerPot)
        {
            foreach (var pair in flowerCounts)
            {
                if (pair.Value < minimumCapacityPerPot || pair.Value % minimumCapacityPerPot != 0)
                {
                    throw new ArgumentException($"Each used color needs a flower count that is a multiple of {minimumCapacityPerPot}.");
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

                var color = ProceduralLevelRandom.PickWeightedColor(eligibleColors, flowerCounts, random);
                potCounts[color]++;
                remainingPots--;
            }

            return potCounts;
        }

        private static List<PotDefinition> BuildPots(
            IReadOnlyDictionary<FlowerColor, int> flowerCounts,
            IReadOnlyDictionary<FlowerColor, int> potColorCounts,
            Random random,
            int capacityUnit)
        {
            var pots = new List<PotDefinition>();
            foreach (var pair in flowerCounts)
            {
                var capacities = SplitCapacityRandomly(pair.Value, potColorCounts[pair.Key], random, capacityUnit);
                pots.AddRange(capacities.Select(capacity => new PotDefinition(pair.Key, capacity)));
            }

            return pots;
        }

        private static IEnumerable<int> SplitCapacityRandomly(
            int totalCapacity,
            int potCount,
            Random random,
            int capacityUnit)
        {
            if (potCount <= 0 || totalCapacity < potCount * capacityUnit || totalCapacity % capacityUnit != 0)
            {
                throw new ArgumentException("Invalid pot split requested.");
            }

            var capacities = Enumerable.Repeat(capacityUnit, potCount).ToList();
            var remainingCapacity = totalCapacity - potCount * capacityUnit;
            while (remainingCapacity > 0)
            {
                capacities[random.Next(capacities.Count)] += capacityUnit;
                remainingCapacity -= capacityUnit;
            }

            ProceduralLevelRandom.Shuffle(capacities, random);
            return capacities;
        }

        private static PotQueueColumnDefinition[] SplitIntoColumns(
            IReadOnlyList<PotDefinition> pots,
            int launchColumnCount,
            int potsPerColumn)
        {
            var columns = new PotQueueColumnDefinition[launchColumnCount];
            var potIndex = 0;
            for (var columnIndex = 0; columnIndex < launchColumnCount; columnIndex++)
            {
                var column = new List<PotDefinition>(potsPerColumn);
                for (var i = 0; i < potsPerColumn; i++)
                {
                    column.Add(pots[potIndex]);
                    potIndex++;
                }

                columns[columnIndex] = new PotQueueColumnDefinition(column);
            }

            return columns;
        }
    }
}
