using System;

namespace BlossomFlow.Gameplay.Domain
{
    public sealed class ConveyorPot
    {
        internal ConveyorPot(PotModel pot, ConveyorPath path, int pathIndex)
        {
            Pot = pot ?? throw new ArgumentNullException(nameof(pot));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            PathIndex = pathIndex;
        }

        public PotModel Pot { get; }
        public ConveyorPath Path { get; }
        public int PathIndex { get; private set; }
        public bool IsPausedForCollection { get; private set; }
        public bool HasReachedEnd => PathIndex == Path.EndIndex;

        public bool TryGetCurrentScan(out ConveyorScan scan)
        {
            return Path.TryGetScan(PathIndex, out scan);
        }

        internal void MoveTo(int pathIndex)
        {
            if (pathIndex < 0 || pathIndex > Path.EndIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(pathIndex), "Conveyor path index is outside the path.");
            }

            PathIndex = pathIndex;
        }

        internal void PauseForCollection()
        {
            IsPausedForCollection = true;
        }

        internal void ResumeFromCollection()
        {
            IsPausedForCollection = false;
        }
    }
}
