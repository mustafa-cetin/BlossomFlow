using System;

namespace BlossomFlow.Gameplay.Domain
{
    public sealed class WaitingSlot
    {
        public WaitingSlot(int index, bool isTemporary = false)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Slot index must be zero or greater.");
            }

            Index = index;
            IsTemporary = isTemporary;
        }

        public int Index { get; }
        public bool IsTemporary { get; }
        public PotModel Pot { get; private set; }
        public bool IsOccupied => Pot != null;

        public void Place(PotModel pot)
        {
            if (pot == null)
            {
                throw new ArgumentNullException(nameof(pot));
            }

            if (IsOccupied)
            {
                throw new InvalidOperationException($"Waiting slot {Index} is already occupied.");
            }

            pot.PlaceInWaiting();
            Pot = pot;
        }

        public void PlaceExistingWaitingPot(PotModel pot)
        {
            if (pot == null)
            {
                throw new ArgumentNullException(nameof(pot));
            }

            if (IsOccupied)
            {
                throw new InvalidOperationException($"Waiting slot {Index} is already occupied.");
            }

            if (pot.State != PotState.Waiting)
            {
                throw new InvalidOperationException($"Pot must already be waiting before being moved between waiting slots, but it is {pot.State}.");
            }

            Pot = pot;
        }

        public PotModel Recall()
        {
            if (!IsOccupied)
            {
                throw new InvalidOperationException($"Waiting slot {Index} is empty.");
            }

            var pot = Pot;
            Pot = null;
            pot.Launch();
            return pot;
        }

        public PotModel RemoveWaitingPot()
        {
            if (!IsOccupied)
            {
                throw new InvalidOperationException($"Waiting slot {Index} is empty.");
            }

            var pot = Pot;
            Pot = null;
            return pot;
        }
    }
}
