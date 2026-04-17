using System;
using System.Collections.Generic;
using System.Linq;

namespace BlossomFlow.Gameplay.Domain
{
    public sealed class WaitingSlots
    {
        private readonly WaitingSlot[] _slots;

        public event Action<WaitingSlots, WaitingSlot, PotModel> PotPlaced;
        public event Action<WaitingSlots, WaitingSlot, PotModel> PotRecalled;

        public WaitingSlots(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Waiting slot capacity must be greater than zero.");
            }

            _slots = Enumerable.Range(0, capacity).Select(index => new WaitingSlot(index)).ToArray();
        }

        public IReadOnlyList<WaitingSlot> Slots => _slots;
        public int Capacity => _slots.Length;
        public int OccupiedCount => _slots.Count(slot => slot.IsOccupied);
        public int EmptyCount => Capacity - OccupiedCount;
        public bool HasEmptySlot => _slots.Any(slot => !slot.IsOccupied);
        public bool IsFull => !HasEmptySlot;

        public WaitingSlot GetSlot(int slotIndex)
        {
            EnsureSlotIndex(slotIndex);
            return _slots[slotIndex];
        }

        public bool TryGetPot(int slotIndex, out PotModel pot)
        {
            var slot = GetSlot(slotIndex);
            pot = slot.Pot;
            return pot != null;
        }

        public bool TryPlace(PotModel pot, out int slotIndex)
        {
            var slot = _slots.FirstOrDefault(candidate => !candidate.IsOccupied);
            if (slot == null)
            {
                slotIndex = -1;
                return false;
            }

            slot.Place(pot);
            slotIndex = slot.Index;
            PotPlaced?.Invoke(this, slot, pot);
            return true;
        }

        public bool TryRecall(int slotIndex, out PotModel pot)
        {
            EnsureSlotIndex(slotIndex);

            var slot = _slots[slotIndex];
            if (!slot.IsOccupied)
            {
                pot = null;
                return false;
            }

            pot = slot.Recall();
            PotRecalled?.Invoke(this, slot, pot);
            return true;
        }

        public PotModel Recall(int slotIndex)
        {
            if (!TryRecall(slotIndex, out var pot))
            {
                throw new InvalidOperationException($"Waiting slot {slotIndex} is empty.");
            }

            return pot;
        }

        private void EnsureSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "Waiting slot index is outside the available slots.");
            }
        }
    }
}
