using System;

namespace BlossomFlow.Gameplay.Domain
{
    public readonly struct ConveyorScan
    {
        public ConveyorScan(ConveyorEdge edge, int lane)
        {
            if (lane < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lane), "Lane must be zero or greater.");
            }

            Edge = edge;
            Lane = lane;
        }

        public ConveyorEdge Edge { get; }
        public int Lane { get; }
    }
}
