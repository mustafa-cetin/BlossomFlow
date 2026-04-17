using System;
using System.Collections.Generic;
using System.Linq;

namespace BlossomFlow.Gameplay.Domain
{
    public sealed class ConveyorPath
    {
        private readonly ConveyorScan[] _scans;

        public ConveyorPath(IEnumerable<ConveyorScan> scans)
        {
            _scans = (scans ?? throw new ArgumentNullException(nameof(scans))).ToArray();

            if (_scans.Length == 0)
            {
                throw new ArgumentException("A conveyor path needs at least one scan point.", nameof(scans));
            }
        }

        public int Count => _scans.Length;
        public int EndIndex => _scans.Length;

        public ConveyorScan this[int index]
        {
            get
            {
                EnsurePathIndex(index);
                return _scans[index];
            }
        }

        public static ConveyorPath CreateGreenhousePath(int gridWidth, int gridHeight)
        {
            if (gridWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gridWidth), "Grid width must be greater than zero.");
            }

            if (gridHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gridHeight), "Grid height must be greater than zero.");
            }

            var scans = new List<ConveyorScan>();

            for (var x = 0; x < gridWidth; x++)
            {
                scans.Add(new ConveyorScan(ConveyorEdge.Bottom, x));
            }

            for (var y = 1; y < gridHeight; y++)
            {
                scans.Add(new ConveyorScan(ConveyorEdge.Right, y));
            }

            for (var x = gridWidth - 2; x >= 0; x--)
            {
                scans.Add(new ConveyorScan(ConveyorEdge.Top, x));
            }

            for (var y = gridHeight - 2; y >= 0; y--)
            {
                scans.Add(new ConveyorScan(ConveyorEdge.Left, y));
            }

            return new ConveyorPath(scans);
        }

        public bool TryGetScan(int index, out ConveyorScan scan)
        {
            if (index < 0 || index >= _scans.Length)
            {
                scan = default(ConveyorScan);
                return false;
            }

            scan = _scans[index];
            return true;
        }

        private void EnsurePathIndex(int index)
        {
            if (index < 0 || index >= _scans.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Conveyor path index is outside the path.");
            }
        }
    }
}
