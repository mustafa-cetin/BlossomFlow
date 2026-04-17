using System;
using System.Collections.Generic;
using System.Linq;

namespace BlossomFlow.Gameplay.Domain.Levels
{
    [Serializable]
    public sealed class ConveyorPathDefinition
    {
        public ConveyorPathDefinition()
            : this("greenhouse")
        {
        }

        public ConveyorPathDefinition(
            string referenceId = null,
            IEnumerable<ConveyorScanDefinition> scans = null)
        {
            ReferenceId = referenceId;
            Scans = (scans ?? Enumerable.Empty<ConveyorScanDefinition>()).ToArray();
        }

        public string ReferenceId;
        public ConveyorScanDefinition[] Scans;

        public bool HasCustomScans => Scans != null && Scans.Length > 0;

        public ConveyorPath CreatePath(int gridWidth, int gridHeight)
        {
            Validate(gridWidth, gridHeight);

            if (!HasCustomScans)
            {
                return ConveyorPath.CreateGreenhousePath(gridWidth, gridHeight);
            }

            return new ConveyorPath(Scans.Select(scan => scan.ToScan()));
        }

        public void Validate(int gridWidth, int gridHeight)
        {
            if (gridWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gridWidth), "Grid width must be greater than zero.");
            }

            if (gridHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gridHeight), "Grid height must be greater than zero.");
            }

            if (!HasCustomScans)
            {
                return;
            }

            for (var i = 0; i < Scans.Length; i++)
            {
                var scan = Scans[i];
                var maxLaneExclusive = scan.Edge == ConveyorEdge.Top || scan.Edge == ConveyorEdge.Bottom
                    ? gridWidth
                    : gridHeight;

                if (scan.Lane < 0 || scan.Lane >= maxLaneExclusive)
                {
                    throw new ArgumentException($"Conveyor scan {i} lane {scan.Lane} is outside the {scan.Edge} edge bounds.");
                }
            }
        }
    }
}
