using System;
using System.Collections.Generic;
using System.Linq;

namespace BlossomFlow.Gameplay.Domain
{
    public sealed class WaitingSlots
    {
        private readonly List<WaitingSlot> _slots;
        private readonly int _baseCapacity;
        private int _nextSlotIndex;

        public event Action<WaitingSlots, WaitingSlot, PotModel> PotPlaced;
        public event Action<WaitingSlots, WaitingSlot, PotModel> PotRecalled;
        public event Action<WaitingSlots, WaitingSlot> TemporarySlotCreated;
        public event Action<WaitingSlots, int> TemporarySlotRemoved;

        public WaitingSlots(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Waiting slot capacity must be greater than zero.");
            }

            _baseCapacity = capacity;
            _nextSlotIndex = capacity;
            _slots = Enumerable.Range(0, capacity).Select(index => new WaitingSlot(index, WaitingSlotKind.Base)).ToList();
        }

        public IReadOnlyList<WaitingSlot> Slots => _slots;
        public int BaseCapacity => _baseCapacity;
        public int UsableCapacity => _slots.Count(slot => slot.IsUsable);
        public int OccupiedCount => _slots.Count(slot => slot.IsOccupied);
        public int OccupiedUsableCount => _slots.Count(slot => slot.IsUsable && slot.IsOccupied);
        public int EmptyUsableCount => _slots.Count(slot => slot.IsUsable && !slot.IsOccupied);
        public bool HasEmptyUsableSlot => _slots.Any(slot => slot.IsUsable && !slot.IsOccupied);
        public bool IsUsableFull => !HasEmptyUsableSlot;

        public WaitingSlot GetSlot(int slotIndex)
        {
            var slot = _slots.FirstOrDefault(candidate => candidate.Index == slotIndex);
            if (slot == null)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "Waiting slot index is outside the available slots.");
            }

            return slot;
        }

        public bool TryGetPot(int slotIndex, out PotModel pot)
        {
            var slot = GetSlot(slotIndex);
            pot = slot.Pot;
            return pot != null;
        }

        public bool TryPlace(PotModel pot, out int slotIndex)
        {
            var slot = _slots.FirstOrDefault(candidate => candidate.IsUsable && !candidate.IsOccupied);
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

        public bool TryPlaceMany(IReadOnlyList<PotModel> pots, out int[] slotIndexes)
        {
            if (pots == null)
            {
                throw new ArgumentNullException(nameof(pots));
            }

            var emptySlots = _slots
                .Where(candidate => candidate.IsUsable && !candidate.IsOccupied)
                .Take(pots.Count)
                .ToArray();

            if (emptySlots.Length < pots.Count)
            {
                slotIndexes = Array.Empty<int>();
                return false;
            }

            slotIndexes = new int[pots.Count];
            for (var i = 0; i < pots.Count; i++)
            {
                var slot = emptySlots[i];
                slot.Place(pots[i]);
                slotIndexes[i] = slot.Index;
                PotPlaced?.Invoke(this, slot, pots[i]);
            }

            return true;
        }

        public bool TryRecall(int slotIndex, out PotModel pot)
        {
            var slot = GetSlot(slotIndex);
            if (!slot.IsOccupied)
            {
                pot = null;
                return false;
            }

            var removeSlot = slot.IsTemporary;
            pot = slot.Recall();
            PotRecalled?.Invoke(this, slot, pot);
            if (removeSlot)
            {
                RemoveTemporarySlot(slot);
            }

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

        public bool TryMoveLastOccupiedUsableSlotToTemporarySlot(
            out PotModel pot,
            out int sourceSlotIndex,
            out int temporarySlotIndex)
        {
            var sourceSlot = _slots.LastOrDefault(candidate => candidate.IsUsable && candidate.IsOccupied);
            if (sourceSlot == null)
            {
                pot = null;
                sourceSlotIndex = -1;
                temporarySlotIndex = -1;
                return false;
            }

            pot = sourceSlot.RemoveWaitingPot();
            var temporarySlot = CreateTemporarySlot();
            temporarySlot.PlaceExistingWaitingPot(pot);
            sourceSlotIndex = sourceSlot.Index;
            temporarySlotIndex = temporarySlot.Index;
            PotPlaced?.Invoke(this, temporarySlot, pot);
            return true;
        }

        public WaitingSlot AddBoosterSlot()
        {
            var slot = new WaitingSlot(_nextSlotIndex, WaitingSlotKind.Booster);
            _nextSlotIndex++;
            _slots.Add(slot);
            return slot;
        }

        public int PlaceInTemporarySlot(PotModel pot)
        {
            if (pot == null)
            {
                throw new ArgumentNullException(nameof(pot));
            }

            if (pot.State != PotState.Launching && pot.State != PotState.Conveying)
            {
                throw new InvalidOperationException($"Pot must be launching or conveying before entering a temporary waiting slot, but it is {pot.State}.");
            }

            var temporarySlot = CreateTemporarySlot();
            temporarySlot.Place(pot);
            PotPlaced?.Invoke(this, temporarySlot, pot);
            return temporarySlot.Index;
        }

        private WaitingSlot CreateTemporarySlot()
        {
            var slot = new WaitingSlot(_nextSlotIndex, WaitingSlotKind.Temporary);
            _nextSlotIndex++;
            _slots.Add(slot);
            TemporarySlotCreated?.Invoke(this, slot);
            return slot;
        }

        private void RemoveTemporarySlot(WaitingSlot slot)
        {
            if (slot == null || slot.Kind != WaitingSlotKind.Temporary)
            {
                return;
            }

            _slots.Remove(slot);
            TemporarySlotRemoved?.Invoke(this, slot.Index);
        }
    }
}
