using System;

namespace BlossomFlow.Gameplay.Domain
{
    public sealed class FlowerCell
    {
        public FlowerCell(GridPosition position, FlowerColor color)
        {
            Position = position;
            Color = color;
            IsOccupied = true;
        }

        public GridPosition Position { get; }
        public FlowerColor Color { get; }
        public bool IsOccupied { get; private set; }

        public void Remove()
        {
            if (!IsOccupied)
            {
                throw new InvalidOperationException($"Flower at {Position} has already been removed.");
            }

            IsOccupied = false;
        }
    }
}
