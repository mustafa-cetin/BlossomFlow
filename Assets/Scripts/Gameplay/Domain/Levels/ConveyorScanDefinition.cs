using System;

namespace BlossomFlow.Gameplay.Domain.Levels
{
    [Serializable]
    public struct ConveyorScanDefinition
    {
        public ConveyorScanDefinition(ConveyorEdge edge, int lane)
        {
            Edge = edge;
            Lane = lane;
        }

        public ConveyorEdge Edge;
        public int Lane;

        public ConveyorScan ToScan()
        {
            return new ConveyorScan(Edge, Lane);
        }
    }
}
