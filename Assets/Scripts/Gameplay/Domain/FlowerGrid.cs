using System;
using System.Collections.Generic;
using System.Linq;

namespace BlossomFlow.Gameplay.Domain
{
    public sealed class FlowerGrid
    {
        private readonly FlowerCell[,] _cells;
        private int _occupiedCount;

        public event Action<FlowerGrid, FlowerCell> FlowerRemoved;
        public event Action<FlowerGrid> Cleared;

        public FlowerGrid(int width, int height, IEnumerable<FlowerCell> cells)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
            }

            Width = width;
            Height = height;
            _cells = new FlowerCell[width, height];

            foreach (var cell in cells ?? Enumerable.Empty<FlowerCell>())
            {
                if (cell == null)
                {
                    throw new ArgumentException("Flower grid cannot contain null cells.", nameof(cells));
                }

                EnsureInBounds(cell.Position);

                if (_cells[cell.Position.X, cell.Position.Y] != null)
                {
                    throw new ArgumentException($"Duplicate flower cell at {cell.Position}.", nameof(cells));
                }

                _cells[cell.Position.X, cell.Position.Y] = cell;
                if (cell.IsOccupied)
                {
                    _occupiedCount++;
                }
            }
        }

        public int Width { get; }
        public int Height { get; }
        public int OccupiedCount => _occupiedCount;
        public bool IsCleared => _occupiedCount == 0;

        public FlowerCell GetCell(GridPosition position)
        {
            EnsureInBounds(position);
            return _cells[position.X, position.Y];
        }

        public IReadOnlyList<FlowerCell> GetAccessibleCells(ConveyorEdge edge)
        {
            var accessibleCells = new List<FlowerCell>();

            switch (edge)
            {
                case ConveyorEdge.Left:
                    AddAccessibleRows(accessibleCells, 0, 1);
                    break;
                case ConveyorEdge.Right:
                    AddAccessibleRows(accessibleCells, Width - 1, -1);
                    break;
                case ConveyorEdge.Top:
                    AddAccessibleColumns(accessibleCells, Height - 1, -1);
                    break;
                case ConveyorEdge.Bottom:
                    AddAccessibleColumns(accessibleCells, 0, 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(edge), edge, "Unsupported conveyor edge.");
            }

            return accessibleCells;
        }

        public IReadOnlyList<FlowerCell> GetAllAccessibleCells()
        {
            var accessibleByPosition = new Dictionary<GridPosition, FlowerCell>();

            AddAccessibleCells(accessibleByPosition, ConveyorEdge.Left);
            AddAccessibleCells(accessibleByPosition, ConveyorEdge.Top);
            AddAccessibleCells(accessibleByPosition, ConveyorEdge.Right);
            AddAccessibleCells(accessibleByPosition, ConveyorEdge.Bottom);

            return accessibleByPosition.Values.ToList();
        }

        public bool TryCollectFirstAccessible(ConveyorScan scan, FlowerColor color, out FlowerCell collected)
        {
            if (TryFindFirstOccupied(scan, out var target) && target.Color == color)
            {
                Remove(target);
                collected = target;
                return true;
            }

            collected = null;
            return false;
        }

        public bool TryFindFirstAccessible(ConveyorScan scan, FlowerColor color, out FlowerCell target)
        {
            if (TryFindFirstOccupied(scan, out var firstOccupied) && firstOccupied.Color == color)
            {
                target = firstOccupied;
                return true;
            }

            target = null;
            return false;
        }

        public bool TryFindFirstAccessible(ConveyorScan scan, out FlowerCell target)
        {
            return TryFindFirstOccupied(scan, out target);
        }

        public bool IsAccessible(GridPosition position, ConveyorEdge edge)
        {
            EnsureInBounds(position);
            var cell = GetCell(position);

            if (cell == null || !cell.IsOccupied)
            {
                return false;
            }

            return edge switch
            {
                ConveyorEdge.Left => IsFirstOccupiedInRow(position, 0, 1),
                ConveyorEdge.Right => IsFirstOccupiedInRow(position, Width - 1, -1),
                ConveyorEdge.Top => IsFirstOccupiedInColumn(position, Height - 1, -1),
                ConveyorEdge.Bottom => IsFirstOccupiedInColumn(position, 0, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(edge), edge, "Unsupported conveyor edge.")
            };
        }

        private void AddAccessibleRows(ICollection<FlowerCell> accessibleCells, int startX, int stepX)
        {
            for (var y = 0; y < Height; y++)
            {
                var cell = FindFirstInRow(y, startX, stepX);
                if (cell != null)
                {
                    accessibleCells.Add(cell);
                }
            }
        }

        private void AddAccessibleColumns(ICollection<FlowerCell> accessibleCells, int startY, int stepY)
        {
            for (var x = 0; x < Width; x++)
            {
                var cell = FindFirstInColumn(x, startY, stepY);
                if (cell != null)
                {
                    accessibleCells.Add(cell);
                }
            }
        }

        private void AddAccessibleCells(IDictionary<GridPosition, FlowerCell> accessibleByPosition, ConveyorEdge edge)
        {
            var accessibleCells = GetAccessibleCells(edge);
            for (var i = 0; i < accessibleCells.Count; i++)
            {
                var cell = accessibleCells[i];
                accessibleByPosition[cell.Position] = cell;
            }
        }

        private void Remove(FlowerCell cell)
        {
            cell.Remove();
            _occupiedCount--;
            FlowerRemoved?.Invoke(this, cell);

            if (IsCleared)
            {
                Cleared?.Invoke(this);
            }
        }

        private bool TryFindFirstOccupied(ConveyorScan scan, out FlowerCell target)
        {
            target = scan.Edge switch
            {
                ConveyorEdge.Left => FindFirstInRow(scan.Lane, 0, 1),
                ConveyorEdge.Right => FindFirstInRow(scan.Lane, Width - 1, -1),
                ConveyorEdge.Top => FindFirstInColumn(scan.Lane, Height - 1, -1),
                ConveyorEdge.Bottom => FindFirstInColumn(scan.Lane, 0, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(scan.Edge), scan.Edge, "Unsupported conveyor edge.")
            };

            return target != null;
        }

        private FlowerCell FindFirstInRow(int y, int startX, int stepX)
        {
            if (y < 0 || y >= Height)
            {
                throw new ArgumentOutOfRangeException(nameof(y), "Scan lane is outside the grid height.");
            }

            for (var x = startX; x >= 0 && x < Width; x += stepX)
            {
                var cell = _cells[x, y];
                if (cell != null && cell.IsOccupied)
                {
                    return cell;
                }
            }

            return null;
        }

        private FlowerCell FindFirstInColumn(int x, int startY, int stepY)
        {
            if (x < 0 || x >= Width)
            {
                throw new ArgumentOutOfRangeException(nameof(x), "Scan lane is outside the grid width.");
            }

            for (var y = startY; y >= 0 && y < Height; y += stepY)
            {
                var cell = _cells[x, y];
                if (cell != null && cell.IsOccupied)
                {
                    return cell;
                }
            }

            return null;
        }

        private bool IsFirstOccupiedInRow(GridPosition position, int startX, int stepX)
        {
            return FindFirstInRow(position.Y, startX, stepX)?.Position.Equals(position) == true;
        }

        private bool IsFirstOccupiedInColumn(GridPosition position, int startY, int stepY)
        {
            return FindFirstInColumn(position.X, startY, stepY)?.Position.Equals(position) == true;
        }

        private void EnsureInBounds(GridPosition position)
        {
            if (position.X < 0 || position.X >= Width || position.Y < 0 || position.Y >= Height)
            {
                throw new ArgumentOutOfRangeException(nameof(position), $"Position {position} is outside a {Width}x{Height} grid.");
            }
        }
    }
}
