using System;

namespace BlossomFlow.Gameplay.Domain.Levels
{
    [Serializable]
    public struct FlowerCellDefinition
    {
        public FlowerCellDefinition(int x, int y, FlowerColor color)
        {
            X = x;
            Y = y;
            Color = color;
        }

        public int X;
        public int Y;
        public FlowerColor Color;
    }
}
