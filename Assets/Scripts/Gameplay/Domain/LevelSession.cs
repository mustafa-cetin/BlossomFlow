using System;
using System.Collections.Generic;

namespace BlossomFlow.Gameplay.Domain
{
    public sealed class LevelSession
    {
        private readonly List<PotModel> _activePots = new();
        private readonly PotQueueColumn[] _launchColumns;

        public event Action<LevelSession, PotModel, int> WaitingPotPlaced;
        public event Action<LevelSession, PotModel, int> WaitingPotRecalled;
        public event Action<LevelSession, PotModel> WaitingOverflowed;
        public event Action<LevelSession, WaitingSlot> TemporaryWaitingSlotCreated;
        public event Action<LevelSession, int> TemporaryWaitingSlotRemoved;
        public event Action<LevelSession, LevelPlayState, LevelPlayState> StateChanged;
        public event Action<LevelSession> LevelWon;
        public event Action<LevelSession, LevelFailureReason, PotModel> LevelFailed;

        public LevelSession(FlowerGrid flowerGrid, WaitingSlots waitingSlots, IEnumerable<PotQueueColumn> launchColumns, ConveyorBelt conveyor = null)
        {
            FlowerGrid = flowerGrid ?? throw new ArgumentNullException(nameof(flowerGrid));
            WaitingSlots = waitingSlots ?? throw new ArgumentNullException(nameof(waitingSlots));
            _launchColumns = new List<PotQueueColumn>(launchColumns ?? throw new ArgumentNullException(nameof(launchColumns))).ToArray();
            Conveyor = conveyor ?? new ConveyorBelt(ConveyorPath.CreateGreenhousePath(FlowerGrid.Width, FlowerGrid.Height));
            WaitingSlots.TemporarySlotCreated += OnTemporaryWaitingSlotCreated;
            WaitingSlots.TemporarySlotRemoved += OnTemporaryWaitingSlotRemoved;

            if (_launchColumns.Length == 0)
            {
                throw new ArgumentException("A level session needs at least one launch column.", nameof(launchColumns));
            }
        }

        public FlowerGrid FlowerGrid { get; }
        public WaitingSlots WaitingSlots { get; }
        public ConveyorBelt Conveyor { get; }
        public IReadOnlyList<PotQueueColumn> LaunchColumns => _launchColumns;
        public IReadOnlyList<PotModel> ActivePots => _activePots;
        public LevelPlayState State { get; private set; } = LevelPlayState.NotStarted;
        public PotModel FailedPot { get; private set; }
        public LevelFailureReason FailureReason { get; private set; } = LevelFailureReason.None;
        public bool IsComplete => State == LevelPlayState.Won || State == LevelPlayState.Failed;

        public void Start()
        {
            if (State != LevelPlayState.NotStarted)
            {
                throw new InvalidOperationException($"Cannot start a level while it is {State}.");
            }

            TransitionTo(LevelPlayState.Playing);
            EvaluateWinCondition();
        }

        public PotModel LaunchFromColumn(int columnIndex)
        {
            EnsurePlaying();
            EnsureColumnIndex(columnIndex);
            EnsureConveyorCanEnter();

            var pot = _launchColumns[columnIndex].LaunchTop();
            _activePots.Add(pot);
            Conveyor.AddPot(pot);
            return pot;
        }

        public PotModel RecallWaitingPot(int slotIndex)
        {
            EnsurePlaying();
            EnsureConveyorCanEnter();

            var recalledFromSlot = slotIndex;
            var pot = WaitingSlots.Recall(slotIndex);
            _activePots.Add(pot);
            Conveyor.AddPot(pot);
            WaitingPotRecalled?.Invoke(this, pot, recalledFromSlot);
            return pot;
        }

        public IReadOnlyList<ConveyorPot> AdvanceConveyor(bool reenterUnfinishedPotsAtEnd = false)
        {
            EnsurePlaying();
            var reachedEnd = Conveyor.Advance();

            if (reenterUnfinishedPotsAtEnd)
            {
                ReenterUnfinishedPotsAtBeltEnd();
                return reachedEnd;
            }

            ResolvePotsAtBeltEnd();

            return reachedEnd;
        }

        private void ResolvePotsAtBeltEnd()
        {
            var conveyorPots = new List<ConveyorPot>(Conveyor.Pots);
            for (var i = 0; i < conveyorPots.Count && State == LevelPlayState.Playing; i++)
            {
                var conveyorPot = conveyorPots[i];
                if (conveyorPot.HasReachedEnd && Conveyor.TryGetPot(conveyorPot.Pot, out _))
                {
                    ResolvePotAtBeltEnd(conveyorPot.Pot, out _);
                }
            }
        }

        private void ReenterUnfinishedPotsAtBeltEnd()
        {
            var conveyorPots = new List<ConveyorPot>(Conveyor.Pots);
            for (var i = 0; i < conveyorPots.Count && State == LevelPlayState.Playing; i++)
            {
                var conveyorPot = conveyorPots[i];
                if (!conveyorPot.HasReachedEnd || !Conveyor.TryGetPot(conveyorPot.Pot, out _))
                {
                    continue;
                }

                if (conveyorPot.Pot.IsFull)
                {
                    ResolvePotAtBeltEnd(conveyorPot.Pot, out _);
                    continue;
                }

                Conveyor.TryReenterReachedEndPot(conveyorPot.Pot);
            }
        }

        public bool TryCollectAtCurrentConveyorPosition(PotModel pot, out FlowerCell collected)
        {
            EnsurePlaying();
            EnsureActive(pot);

            if (!Conveyor.TryGetPot(pot, out var conveyorPot) || !conveyorPot.TryGetCurrentScan(out var scan))
            {
                collected = null;
                return false;
            }

            return TryCollectFromScan(pot, scan, out collected);
        }

        public bool TryCollectFromScan(PotModel pot, ConveyorScan scan, out FlowerCell collected)
        {
            EnsurePlaying();
            EnsureActive(pot);

            if (pot.State == PotState.Launching)
            {
                pot.BeginConveying();
            }

            if (pot.State != PotState.Conveying)
            {
                throw new InvalidOperationException($"Pot must be conveying before collecting, but it is {pot.State}.");
            }

            if (!FlowerGrid.TryCollectFirstAccessible(scan, pot.Color, out collected))
            {
                return false;
            }

            if (Conveyor.TryGetPot(pot, out _))
            {
                if (!Conveyor.TryBeginCollection(pot))
                {
                    throw new InvalidOperationException("Conveyor pot could not pause for collection.");
                }

                Conveyor.CompleteCollection(pot);
            }
            else
            {
                pot.BeginCollecting();
                pot.CollectOne();
            }

            if (pot.IsFull)
            {
                if (Conveyor.TryGetPot(pot, out _))
                {
                    Conveyor.RemovePot(pot);
                }

                _activePots.Remove(pot);
                EvaluateWinCondition();
            }

            return true;
        }

        public bool ResolvePotAtBeltEnd(PotModel pot, out int waitingSlotIndex)
        {
            EnsurePlaying();
            EnsureActive(pot);

            if (Conveyor.TryGetPot(pot, out var conveyorPot) && !conveyorPot.HasReachedEnd)
            {
                throw new InvalidOperationException("Pot has not reached the conveyor end.");
            }

            if (pot.State == PotState.Launching)
            {
                pot.BeginConveying();
            }

            if (pot.IsFull)
            {
                if (Conveyor.TryGetPot(pot, out _))
                {
                    Conveyor.RemovePot(pot);
                }

                _activePots.Remove(pot);
                waitingSlotIndex = -1;
                EvaluateWinCondition();
                return true;
            }

            if (WaitingSlots.TryPlace(pot, out waitingSlotIndex))
            {
                if (Conveyor.TryGetPot(pot, out _))
                {
                    Conveyor.RemovePot(pot);
                }

                _activePots.Remove(pot);
                WaitingPotPlaced?.Invoke(this, pot, waitingSlotIndex);
                EvaluateWinCondition();
                return true;
            }

            FailedPot = pot;
            FailureReason = LevelFailureReason.WaitingSlotsFull;
            TransitionTo(LevelPlayState.Failed);
            WaitingOverflowed?.Invoke(this, pot);
            LevelFailed?.Invoke(this, FailureReason, pot);
            waitingSlotIndex = -1;
            return false;
        }

        public bool TryContinueAfterWaitingOverflow()
        {
            if (State != LevelPlayState.Failed ||
                FailureReason != LevelFailureReason.WaitingSlotsFull ||
                FailedPot == null ||
                FailedPot.IsTerminal)
            {
                return false;
            }

            if (!TryActivateOverflowRescue(FailedPot, out _))
            {
                return false;
            }

            FailedPot = null;
            FailureReason = LevelFailureReason.None;
            TransitionTo(LevelPlayState.Playing);
            EvaluateWinCondition();
            return true;
        }

        private bool TryActivateOverflowRescue(PotModel overflowPot, out int overflowPotSlotIndex)
        {
            overflowPotSlotIndex = -1;
            if (!WaitingSlots.IsFull || Conveyor.Pots.Count == 0)
            {
                return false;
            }

            if (!WaitingSlots.TryMoveToTemporarySlot(
                WaitingSlots.Capacity - 1,
                out var relocatedWaitingPot,
                out var relocatedTemporarySlotIndex))
            {
                return false;
            }

            WaitingPotRecalled?.Invoke(this, relocatedWaitingPot, WaitingSlots.Capacity - 1);
            WaitingPotPlaced?.Invoke(this, relocatedWaitingPot, relocatedTemporarySlotIndex);

            var conveyorPots = new List<ConveyorPot>(Conveyor.Pots);
            for (var i = 0; i < conveyorPots.Count; i++)
            {
                var conveyorPot = conveyorPots[i];
                if (!Conveyor.TryGetPot(conveyorPot.Pot, out _))
                {
                    continue;
                }

                var temporarySlotIndex = WaitingSlots.PlaceInTemporarySlot(conveyorPot.Pot);
                Conveyor.RemovePot(conveyorPot.Pot);
                _activePots.Remove(conveyorPot.Pot);
                WaitingPotPlaced?.Invoke(this, conveyorPot.Pot, temporarySlotIndex);

                if (ReferenceEquals(conveyorPot.Pot, overflowPot))
                {
                    overflowPotSlotIndex = temporarySlotIndex;
                }
            }

            return true;
        }

        private void OnTemporaryWaitingSlotCreated(WaitingSlots waitingSlots, WaitingSlot slot)
        {
            TemporaryWaitingSlotCreated?.Invoke(this, slot);
        }

        private void OnTemporaryWaitingSlotRemoved(WaitingSlots waitingSlots, int slotIndex)
        {
            TemporaryWaitingSlotRemoved?.Invoke(this, slotIndex);
        }

        private void EnsureConveyorCanEnter()
        {
            if (!Conveyor.CanEnter)
            {
                throw new InvalidOperationException("Cannot enter the conveyor while its start is occupied.");
            }
        }

        private void EvaluateWinCondition()
        {
            if (State == LevelPlayState.Playing &&
                FlowerGrid.IsCleared &&
                _activePots.Count == 0 &&
                WaitingSlots.OccupiedCount == 0 &&
                AreLaunchColumnsEmpty())
            {
                TransitionTo(LevelPlayState.Won);
                LevelWon?.Invoke(this);
            }
        }

        private bool AreLaunchColumnsEmpty()
        {
            for (var i = 0; i < _launchColumns.Length; i++)
            {
                if (_launchColumns[i].CanLaunch)
                {
                    return false;
                }
            }

            return true;
        }

        private void TransitionTo(LevelPlayState nextState)
        {
            if (State == nextState)
            {
                return;
            }

            if (!CanTransition(State, nextState))
            {
                throw new InvalidOperationException($"Cannot transition level from {State} to {nextState}.");
            }

            var previousState = State;
            State = nextState;
            StateChanged?.Invoke(this, previousState, nextState);
        }

        private static bool CanTransition(LevelPlayState from, LevelPlayState to)
        {
            return from switch
            {
                LevelPlayState.NotStarted => to == LevelPlayState.Playing,
                LevelPlayState.Playing => to == LevelPlayState.Won || to == LevelPlayState.Failed,
                LevelPlayState.Won => false,
                LevelPlayState.Failed => to == LevelPlayState.Playing,
                _ => false
            };
        }

        private void EnsurePlaying()
        {
            if (State != LevelPlayState.Playing)
            {
                throw new InvalidOperationException($"Gameplay action is not allowed while level is {State}.");
            }
        }

        private void EnsureActive(PotModel pot)
        {
            if (pot == null)
            {
                throw new ArgumentNullException(nameof(pot));
            }

            if (!_activePots.Contains(pot))
            {
                throw new InvalidOperationException("Pot is not active in this level session.");
            }
        }

        private void EnsureColumnIndex(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= _launchColumns.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndex), "Launch column index is outside the available columns.");
            }
        }
    }
}
