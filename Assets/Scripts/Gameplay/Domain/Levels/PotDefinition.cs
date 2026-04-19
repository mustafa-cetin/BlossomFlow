using System;

namespace BlossomFlow.Gameplay.Domain.Levels
{
    [Serializable]
    public struct PotDefinition
    {
        public PotDefinition(FlowerColor color, int capacity, int linkedPairId = 0)
        {
            Color = color;
            Capacity = capacity;
            LinkedPairId = linkedPairId;
        }

        public FlowerColor Color;
        public int Capacity;
        public int LinkedPairId;
    }
}
