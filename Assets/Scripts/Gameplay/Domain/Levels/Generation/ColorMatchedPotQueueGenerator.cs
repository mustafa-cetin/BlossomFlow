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
            var capacityUnit = settings.PreferCapacityMultiplesOfTen
                ? PickPreferredCapacityUnit(flowerCounts, totalPots)
                : 1;
            var potColorCounts = AllocatePotCountsByColor(flowerCounts, totalPots, random, capacityUnit);
            var pots = BuildPots(flowerCounts, potColorCounts, random, capacityUnit);
            ProceduralLevelRandom.Shuffle(pots, random);

            return SplitIntoColumns(pots, settings.LaunchColumnCount, settings.PotsPerColumn);
        }

        private static int PickPreferredCapacityUnit(
            IReadOnlyDictionary<FlowerColor, int> flowerCounts,
            int totalPots)
        {
            if (CanAllocateWithCapacityUnit(flowerCounts, totalPots, 10))
            {
                return 10;
            }

            return CanAllocateWithCapacityUnit(flowerCounts, totalPots, 5) ? 5 : 1;
        }

        private static bool CanAllocateWithCapacityUnit(
            IReadOnlyDictionary<FlowerColor, int> flowerCounts,
            int totalPots,
            int capacityUnit)
        {
            return totalPots <= flowerCounts.Sum(pair => MaximumPotCountForCapacityUnit(pair.Value, capacityUnit));
        }

        private static Dictionary<FlowerColor, int> AllocatePotCountsByColor(
            IReadOnlyDictionary<FlowerColor, int> flowerCounts,
            int totalPots,
            Random random,
            int capacityUnit)
        {
            var potCounts = flowerCounts.ToDictionary(pair => pair.Key, _ => 1);
            var remainingPots = totalPots - potCounts.Count;

            while (remainingPots > 0)
            {
                var eligibleColors = flowerCounts
                    .Where(pair => potCounts[pair.Key] < MaximumPotCountForCapacityUnit(pair.Value, capacityUnit))
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

        private static int MaximumPotCountForCapacityUnit(int totalCapacity, int capacityUnit)
        {
            return capacityUnit <= 1
                ? totalCapacity
                : Math.Max(1, (totalCapacity + capacityUnit - 1) / capacityUnit);
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
            if (potCount <= 0 || totalCapacity < potCount)
            {
                throw new ArgumentException("Invalid pot split requested.");
            }

            if (capacityUnit <= 1)
            {
                var capacitiesN = Enumerable.Repeat(1, potCount).ToList();
                var remainingCapacity = totalCapacity - potCount;
                while (remainingCapacity > 0)
                {
                    capacitiesN[random.Next(capacitiesN.Count)]++;
                    remainingCapacity--;
                }

                ProceduralLevelRandom.Shuffle(capacitiesN, random);
                return capacitiesN;
            }

            if (potCount > MaximumPotCountForCapacityUnit(totalCapacity, capacityUnit))
            {
                throw new ArgumentException("Invalid preferred pot split requested.");
            }

            var capacities = new List<int>();
            var remainder = totalCapacity % capacityUnit;
            var fullUnits = totalCapacity / capacityUnit;

            if (remainder > 0)
            {
                capacities.Add(remainder);
            }

            capacities.AddRange(Enumerable.Repeat(capacityUnit, fullUnits));

            while (capacities.Count > potCount)
            {
                var mergeableIndexes = capacities
                    .Select((capacity, index) => (capacity, index))
                    .Where(pair => pair.capacity % capacityUnit == 0)
                    .Select(pair => pair.index)
                    .ToList();

                int fromIndex;
                int toIndex;
                if (mergeableIndexes.Count >= 2)
                {
                    fromIndex = mergeableIndexes[random.Next(mergeableIndexes.Count)];
                    var targetChoices = mergeableIndexes.Where(index => index != fromIndex).ToList();
                    toIndex = targetChoices[random.Next(targetChoices.Count)];
                }
                else
                {
                    fromIndex = random.Next(capacities.Count);
                    toIndex = random.Next(capacities.Count - 1);
                    if (toIndex >= fromIndex)
                    {
                        toIndex++;
                    }
                }

                capacities[toIndex] += capacities[fromIndex];
                capacities.RemoveAt(fromIndex);
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
