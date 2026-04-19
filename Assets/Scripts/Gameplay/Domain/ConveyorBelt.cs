using System;
using System.Collections.Generic;
using System.Linq;

namespace BlossomFlow.Gameplay.Domain
{
    public sealed class ConveyorBelt
    {
        public const int DefaultMaxPots = 5;
        private const int LinkedPotEntrySpacing = 5;

        private readonly List<ConveyorPot> _pots = new();

        public event Action<ConveyorBelt, ConveyorPot> PotEntered;
        public event Action<ConveyorBelt, ConveyorPot> PotAdvanced;
        public event Action<ConveyorBelt, ConveyorPot> PotPausedForCollection;
        public event Action<ConveyorBelt, ConveyorPot> PotResumedFromCollection;
        public event Action<ConveyorBelt, ConveyorPot> PotReachedEnd;
        public event Action<ConveyorBelt, ConveyorPot> PotRemoved;

        public ConveyorBelt(ConveyorPath path, int maxPots = DefaultMaxPots)
        {
            if (maxPots <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPots), "Conveyor capacity must be greater than zero.");
            }

            Path = path ?? throw new ArgumentNullException(nameof(path));
            MaxPots = maxPots;
        }

        public ConveyorPath Path { get; }
        public int MaxPots { get; }
        public IReadOnlyList<ConveyorPot> Pots => _pots;
        public bool CanEnter => CanEnterCount(1);

        public bool CanEnterCount(int potCount)
        {
            if (potCount <= 0)
            {
                return false;
            }

            if (_pots.Count + potCount > MaxPots)
            {
                return false;
            }

            var requiredEntrySpan = GetRequiredEntrySpan(potCount);
            if (requiredEntrySpan - 1 > Path.EndIndex)
            {
                return false;
            }

            for (var pathIndex = 0; pathIndex < requiredEntrySpan; pathIndex++)
            {
                if (IsPathIndexOccupied(pathIndex))
                {
                    return false;
                }
            }

            return true;
        }

        public ConveyorPot AddPot(PotModel pot)
        {
            if (pot == null)
            {
                throw new ArgumentNullException(nameof(pot));
            }

            if (!CanEnterCount(1))
            {
                throw new InvalidOperationException("Cannot add a pot while the conveyor is full or its start is occupied.");
            }

            return AddPotAtEntryIndex(pot, 0);
        }

        public IReadOnlyList<ConveyorPot> AddPots(IReadOnlyList<PotModel> pots)
        {
            if (pots == null)
            {
                throw new ArgumentNullException(nameof(pots));
            }

            if (!CanEnterCount(pots.Count))
            {
                throw new InvalidOperationException("Cannot add pots while the conveyor lacks enough entry space.");
            }

            var conveyorPots = new List<ConveyorPot>(pots.Count);
            for (var i = 0; i < pots.Count; i++)
            {
                conveyorPots.Add(AddPotAtEntryIndex(pots[i], GetEntryIndex(i, pots.Count)));
            }

            return conveyorPots;
        }

        private static int GetRequiredEntrySpan(int potCount)
        {
            return potCount <= 1 ? 1 : (potCount - 1) * LinkedPotEntrySpacing + 1;
        }

        private static int GetEntryIndex(int potIndex, int potCount)
        {
            return potCount <= 1 ? 0 : potIndex * LinkedPotEntrySpacing;
        }

        private ConveyorPot AddPotAtEntryIndex(PotModel pot, int pathIndex)
        {
            if (pot == null)
            {
                throw new ArgumentNullException(nameof(pot));
            }

            if (pot.State == PotState.Launching)
            {
                pot.BeginConveying();
            }
            else if (pot.State != PotState.Conveying)
            {
                throw new InvalidOperationException($"Pot must be launching or conveying before entering the conveyor, but it is {pot.State}.");
            }

            var conveyorPot = new ConveyorPot(pot, Path, pathIndex);
            _pots.Add(conveyorPot);
            PotEntered?.Invoke(this, conveyorPot);
            return conveyorPot;
        }

        public IReadOnlyList<ConveyorPot> Advance()
        {
            var reachedEnd = new List<ConveyorPot>();
            var movingPots = _pots
                .Where(pot => !pot.HasReachedEnd)
                .OrderByDescending(pot => pot.PathIndex)
                .ToArray();

            for (var i = 0; i < movingPots.Length; i++)
            {
                var conveyorPot = movingPots[i];

                if (conveyorPot.IsPausedForCollection)
                {
                    continue;
                }

                var nextIndex = conveyorPot.PathIndex + 1;
                if (IsPathIndexOccupied(nextIndex))
                {
                    continue;
                }

                conveyorPot.MoveTo(nextIndex);

                if (conveyorPot.HasReachedEnd)
                {
                    reachedEnd.Add(conveyorPot);
                    PotReachedEnd?.Invoke(this, conveyorPot);
                }
                else
                {
                    PotAdvanced?.Invoke(this, conveyorPot);
                }
            }

            return reachedEnd;
        }

        public bool TryBeginCollection(PotModel pot)
        {
            if (!TryGetPot(pot, out var conveyorPot))
            {
                return false;
            }

            if (conveyorPot.IsPausedForCollection || conveyorPot.HasReachedEnd)
            {
                return false;
            }

            pot.BeginCollecting();
            conveyorPot.PauseForCollection();
            PotPausedForCollection?.Invoke(this, conveyorPot);
            return true;
        }

        public void CompleteCollection(PotModel pot)
        {
            var conveyorPot = GetPot(pot);

            if (!conveyorPot.IsPausedForCollection)
            {
                throw new InvalidOperationException("Cannot complete collection for a pot that is not paused.");
            }

            pot.CollectOne();
            conveyorPot.ResumeFromCollection();
            PotResumedFromCollection?.Invoke(this, conveyorPot);
        }

        public ConveyorPot RemovePot(PotModel pot)
        {
            var conveyorPot = GetPot(pot);
            _pots.Remove(conveyorPot);
            PotRemoved?.Invoke(this, conveyorPot);
            return conveyorPot;
        }

        public bool TryReenterReachedEndPot(PotModel pot)
        {
            var conveyorPot = GetPot(pot);
            if (!conveyorPot.HasReachedEnd)
            {
                throw new InvalidOperationException("Pot has not reached the conveyor end.");
            }

            if (IsPathIndexOccupied(0))
            {
                return false;
            }

            _pots.Remove(conveyorPot);
            PotRemoved?.Invoke(this, conveyorPot);
            AddPot(pot);
            return true;
        }

        public bool TryGetPot(PotModel pot, out ConveyorPot conveyorPot)
        {
            conveyorPot = _pots.FirstOrDefault(candidate => ReferenceEquals(candidate.Pot, pot));
            return conveyorPot != null;
        }

        private ConveyorPot GetPot(PotModel pot)
        {
            if (pot == null)
            {
                throw new ArgumentNullException(nameof(pot));
            }

            if (!TryGetPot(pot, out var conveyorPot))
            {
                throw new InvalidOperationException("Pot is not on this conveyor.");
            }

            return conveyorPot;
        }

        private bool IsPathIndexOccupied(int pathIndex)
        {
            return _pots.Any(pot => pot.PathIndex == pathIndex);
        }
    }
}
