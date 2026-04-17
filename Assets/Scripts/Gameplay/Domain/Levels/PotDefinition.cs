using System;

namespace BlossomFlow.Gameplay.Domain.Levels
{
    [Serializable]
    public struct PotDefinition
    {
        public PotDefinition(FlowerColor color, int capacity)
        {
            Color = color;
            Capacity = capacity;
        }

        public FlowerColor Color;
        public int Capacity;
    }
}
