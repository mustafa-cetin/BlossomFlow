using System;
using System.Collections.Generic;
using BlossomFlow.Gameplay.Domain.Boosters;

namespace BlossomFlow.Gameplay.Domain
{
    public sealed class LevelSession
    {
        private readonly List<PotModel> _activePots = new();
        private readonly Dictionary<int, int> _linkedGroupSizes = new();
        private readonly PotQueueColumn[] _launchColumns;

        public event Action<LevelSession, PotModel, int> WaitingPotPlaced;
        public event Action<LevelSession, PotModel, int> WaitingPotRecalled;
        public event Action<LevelSession, PotModel> WaitingOverflowed;
        public event Action<LevelSession, WaitingSlot> TemporaryWaitingSlotCreated;
        public event Action<LevelSession, int> TemporaryWaitingSlotRemoved;
        public event Action<LevelSession, WaitingSlot> ExtraWaitingSlotCreated;
        public event Action<LevelSession, LevelPlayState, LevelPlayState> StateChanged;
        public event Action<LevelSession> LevelWon;
        public event Action<LevelSession, LevelFailureReason, PotModel> LevelFailed;

        public LevelSession(FlowerGrid flowerGrid, WaitingSlots waitingSlots, IEnumerable<PotQueueColumn> launchColumns, ConveyorBelt conveyor = null)
        {
            FlowerGrid = flowerGrid ?? throw new ArgumentNullException(nameof(flowerGrid));
            WaitingSlots = waitingSlots ?? throw new ArgumentNullException(nameof(waitingSlots));
            _launchColumns = new List<PotQueueColumn>(launchColumns ?? throw new ArgumentNullException(nameof(launchColumns))).ToArray();
            BuildLinkedGroupSizes();
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

        public bool IsActive(PotModel pot)
        {
            return pot != null && _activePots.Contains(pot);
        }

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
            var launchedPots = LaunchGroupFromColumn(columnIndex, out _);
            return launchedPots[0];
        }

        public IReadOnlyList<PotModel> LaunchGroupFromColumn(int columnIndex, out IReadOnlyList<int> sourceColumnIndexes)
        {
            EnsurePlaying();
            EnsureColumnIndex(columnIndex);
            EnsureLaunchableColumn(columnIndex);

            var launchColumnIndexes = GetLaunchColumnGroupIndexes(columnIndex);
            EnsureConveyorCanEnter(launchColumnIndexes.Count);

            var launchedPots = new List<PotModel>(launchColumnIndexes.Count);
            for (var i = 0; i < launchColumnIndexes.Count; i++)
            {
                var pot = _launchColumns[launchColumnIndexes[i]].LaunchTop();
                launchedPots.Add(pot);
                _activePots.Add(pot);
            }

            Conveyor.AddPots(launchedPots);
            sourceColumnIndexes = launchColumnIndexes;
            return launchedPots;
        }

        public bool CanLaunchFromColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= _launchColumns.Length || !_launchColumns[columnIndex].CanLaunch)
            {
                return false;
            }

            var pot = _launchColumns[columnIndex].PeekTop();
            if (!pot.IsLinked)
            {
                return true;
            }

            return TryGetLinkedLaunchGroupColumnIndexes(pot, out _);
        }

        public bool TryAddExtraWaitingSlot(out int slotIndex)
        {
            slotIndex = -1;
            if (State != LevelPlayState.Playing)
            {
                return false;
            }

            var slot = WaitingSlots.AddBoosterSlot();
            slotIndex = slot.Index;
            ExtraWaitingSlotCreated?.Invoke(this, slot);
            return true;
        }

        public bool TryShuffleLaunchQueues(IRandomNumberGenerator random)
        {
            EnsurePlaying();
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var shuffledPots = new List<PotModel>();
            for (var i = 0; i < _launchColumns.Length; i++)
            {
                _launchColumns[i].AppendUnlinkedPots(shuffledPots);
            }

            if (shuffledPots.Count <= 1)
            {
                return false;
            }

            var originalPots = new List<PotModel>(shuffledPots);
            ShuffleUntilChanged(shuffledPots, originalPots, random);

            var shuffledIndex = 0;
            for (var i = 0; i < _launchColumns.Length; i++)
            {
                _launchColumns[i].ReplaceUnlinkedPots(shuffledPots, ref shuffledIndex);
            }

            return true;
        }

        private static void ShuffleUntilChanged<T>(IList<T> values, IReadOnlyList<T> originalValues, IRandomNumberGenerator random)
        {
            const int maxAttempts = 4;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                Shuffle(values, random);
                if (!HasSameOrder(values, originalValues))
                {
                    return;
                }
            }

            (values[0], values[1]) = (values[1], values[0]);
        }

        private static bool HasSameOrder<T>(IList<T> values, IReadOnlyList<T> otherValues)
        {
            if (values == null || otherValues == null || values.Count != otherValues.Count)
            {
                return false;
            }

            for (var i = 0; i < values.Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(values[i], otherValues[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetLaunchGroupSize(int columnIndex)
        {
            EnsureColumnIndex(columnIndex);
            if (!_launchColumns[columnIndex].CanLaunch)
            {
                return 0;
            }

            var pot = _launchColumns[columnIndex].PeekTop();
            if (!pot.IsLinked)
            {
                return 1;
            }

            return TryGetLinkedLaunchGroupColumnIndexes(pot, out var columnIndexes)
                ? columnIndexes.Count
                : 0;
        }

        public PotModel RecallWaitingPot(int slotIndex)
        {
            var recalledPots = RecallWaitingPotGroup(slotIndex, out _);
            return recalledPots[0];
        }

        public IReadOnlyList<PotModel> RecallWaitingPotGroup(int slotIndex, out IReadOnlyList<int> recalledSlotIndexes)
        {
            EnsurePlaying();
            if (!WaitingSlots.TryGetPot(slotIndex, out var selectedPot))
            {
                throw new InvalidOperationException($"Waiting slot {slotIndex} is empty.");
            }

            if (!TryGetRecallSlotGroup(selectedPot, slotIndex, out var slotIndexes))
            {
                throw new InvalidOperationException("All linked waiting pots must be in waiting slots before recall.");
            }

            EnsureConveyorCanEnter(slotIndexes.Count);

            var recalledPots = new List<PotModel>(slotIndexes.Count);
            for (var i = 0; i < slotIndexes.Count; i++)
            {
                var recalledFromSlot = slotIndexes[i];
                var pot = WaitingSlots.Recall(recalledFromSlot);
                recalledPots.Add(pot);
                _activePots.Add(pot);
                WaitingPotRecalled?.Invoke(this, pot, recalledFromSlot);
            }

            Conveyor.AddPots(recalledPots);
            recalledSlotIndexes = slotIndexes;
            return recalledPots;
        }

        public bool CanRecallWaitingPot(int slotIndex)
        {
            return WaitingSlots.TryGetPot(slotIndex, out var pot) &&
                TryGetRecallSlotGroup(pot, slotIndex, out _);
        }

        public int GetWaitingRecallGroupSize(int slotIndex)
        {
            if (!WaitingSlots.TryGetPot(slotIndex, out var pot))
            {
                return 0;
            }

            return TryGetRecallSlotGroup(pot, slotIndex, out var slotIndexes)
                ? slotIndexes.Count
                : 0;
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

                if (ShouldResolvePotAtBeltEndDuringFastForward(conveyorPot.Pot))
                {
                    ResolvePotAtBeltEnd(conveyorPot.Pot, out _);
                    continue;
                }

                Conveyor.TryReenterReachedEndPot(conveyorPot.Pot);
            }
        }

        private bool ShouldResolvePotAtBeltEndDuringFastForward(PotModel pot)
        {
            if (!pot.IsFull)
            {
                return false;
            }

            if (!pot.IsLinked)
            {
                return true;
            }

            var linkedPots = GetActiveLinkedGroup(pot);
            return IsCompleteLinkedGroup(pot, linkedPots) && AreAllPotsFull(linkedPots);
        }

        public bool TryCollectAtCurrentConveyorPosition(PotModel pot, out FlowerCell collected)
        {
            EnsurePlaying();
            EnsureActive(pot);

            if (pot.IsFull)
            {
                collected = null;
                return false;
            }

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
                if (pot.IsLinked)
                {
                    var linkedPots = GetActiveLinkedGroup(pot);
                    if (!IsCompleteLinkedGroup(pot, linkedPots) || !AreAllPotsFull(linkedPots))
                    {
                        return true;
                    }

                    RemoveActiveLinkedPots(linkedPots);
                    EvaluateWinCondition();
                    return true;
                }

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

            if (pot.IsLinked)
            {
                var linkedPots = GetActiveLinkedGroup(pot);
                if (!IsCompleteLinkedGroup(pot, linkedPots))
                {
                    waitingSlotIndex = -1;
                    return true;
                }

                return ResolveLinkedPotsAtBeltEnd(linkedPots, out waitingSlotIndex);
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

        private bool ResolveLinkedPotsAtBeltEnd(IReadOnlyList<PotModel> linkedPots, out int waitingSlotIndex)
        {
            for (var i = 0; i < linkedPots.Count; i++)
            {
                if (linkedPots[i].State == PotState.Launching)
                {
                    linkedPots[i].BeginConveying();
                }
            }

            if (AreAllPotsFull(linkedPots))
            {
                RemoveActiveLinkedPots(linkedPots);
                waitingSlotIndex = -1;
                EvaluateWinCondition();
                return true;
            }

            var waitingPots = new List<PotModel>(linkedPots);

            if (WaitingSlots.TryPlaceMany(waitingPots, out var waitingSlotIndexes))
            {
                RemoveActiveLinkedPots(linkedPots);
                for (var i = 0; i < waitingPots.Count; i++)
                {
                    WaitingPotPlaced?.Invoke(this, waitingPots[i], waitingSlotIndexes[i]);
                }

                waitingSlotIndex = waitingSlotIndexes.Length > 0 ? waitingSlotIndexes[0] : -1;
                EvaluateWinCondition();
                return true;
            }

            var failedPot = linkedPots.Count > 0 ? linkedPots[0] : null;
            FailedPot = failedPot;
            FailureReason = LevelFailureReason.WaitingSlotsFull;
            TransitionTo(LevelPlayState.Failed);
            WaitingOverflowed?.Invoke(this, failedPot);
            LevelFailed?.Invoke(this, FailureReason, failedPot);
            waitingSlotIndex = -1;
            return false;
        }

        private void RemoveActiveLinkedPots(IReadOnlyList<PotModel> pots)
        {
            for (var i = 0; i < pots.Count; i++)
            {
                if (Conveyor.TryGetPot(pots[i], out _))
                {
                    Conveyor.RemovePot(pots[i]);
                }

                _activePots.Remove(pots[i]);
            }
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
            if (Conveyor.Pots.Count == 0)
            {
                return false;
            }

            if (!WaitingSlots.HasEmptyUsableSlot)
            {
                if (!WaitingSlots.TryMoveLastOccupiedUsableSlotToTemporarySlot(
                    out var relocatedWaitingPot,
                    out var relocatedSourceSlotIndex,
                    out var relocatedTemporarySlotIndex))
                {
                    return false;
                }

                WaitingPotRecalled?.Invoke(this, relocatedWaitingPot, relocatedSourceSlotIndex);
                WaitingPotPlaced?.Invoke(this, relocatedWaitingPot, relocatedTemporarySlotIndex);
            }

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

        private void EnsureConveyorCanEnter(int potCount = 1)
        {
            if (!Conveyor.CanEnterCount(potCount))
            {
                throw new InvalidOperationException("Cannot enter the conveyor while its start is occupied.");
            }
        }

        private void BuildLinkedGroupSizes()
        {
            _linkedGroupSizes.Clear();
            for (var columnIndex = 0; columnIndex < _launchColumns.Length; columnIndex++)
            {
                var pots = _launchColumns[columnIndex].Pots;
                for (var potIndex = 0; potIndex < pots.Count; potIndex++)
                {
                    var pot = pots[potIndex];
                    if (!pot.IsLinked)
                    {
                        continue;
                    }

                    _linkedGroupSizes.TryGetValue(pot.LinkedPairId, out var count);
                    _linkedGroupSizes[pot.LinkedPairId] = count + 1;
                }
            }
        }

        private void EnsureLaunchableColumn(int columnIndex)
        {
            if (!CanLaunchFromColumn(columnIndex))
            {
                throw new InvalidOperationException("Cannot launch a linked pot until all linked pots are at the front of their columns.");
            }
        }

        private IReadOnlyList<int> GetLaunchColumnGroupIndexes(int columnIndex)
        {
            var pot = _launchColumns[columnIndex].PeekTop();
            if (!pot.IsLinked)
            {
                return new[] { columnIndex };
            }

            if (!TryGetLinkedLaunchGroupColumnIndexes(pot, out var columnIndexes))
            {
                throw new InvalidOperationException($"Linked pot group {pot.LinkedPairId} is missing a frontmost member.");
            }

            return columnIndexes;
        }

        private bool TryGetLinkedLaunchGroupColumnIndexes(PotModel pot, out IReadOnlyList<int> columnIndexes)
        {
            columnIndexes = Array.Empty<int>();

            if (pot == null || !pot.IsLinked)
            {
                return false;
            }

            var groupColumnIndexes = new List<int>();
            for (var columnIndex = 0; columnIndex < _launchColumns.Length; columnIndex++)
            {
                var pots = _launchColumns[columnIndex].Pots;
                for (var potIndex = 0; potIndex < pots.Count; potIndex++)
                {
                    var candidate = pots[potIndex];
                    if (candidate.LinkedPairId != pot.LinkedPairId)
                    {
                        continue;
                    }

                    if (!_launchColumns[columnIndex].IsTop(candidate))
                    {
                        return false;
                    }

                    groupColumnIndexes.Add(columnIndex);
                    break;
                }
            }

            if (groupColumnIndexes.Count != GetLinkedGroupSize(pot.LinkedPairId))
            {
                return false;
            }

            groupColumnIndexes.Sort();
            columnIndexes = groupColumnIndexes;
            return true;
        }

        private IReadOnlyList<PotModel> GetActiveLinkedGroup(PotModel pot)
        {
            if (pot == null || !pot.IsLinked)
            {
                return Array.Empty<PotModel>();
            }

            var group = new List<PotModel>();
            for (var i = 0; i < _activePots.Count; i++)
            {
                var candidate = _activePots[i];
                if (candidate.LinkedPairId == pot.LinkedPairId)
                {
                    group.Add(candidate);
                }
            }

            return group;
        }

        private bool TryGetRecallSlotGroup(PotModel pot, int slotIndex, out IReadOnlyList<int> slotIndexes)
        {
            if (pot == null)
            {
                slotIndexes = Array.Empty<int>();
                return false;
            }

            if (!pot.IsLinked)
            {
                slotIndexes = new[] { slotIndex };
                return true;
            }

            var groupSlotIndexes = new List<int>();
            for (var i = 0; i < WaitingSlots.Slots.Count; i++)
            {
                var slot = WaitingSlots.Slots[i];
                var candidate = slot.Pot;
                if (candidate == null ||
                    candidate.LinkedPairId != pot.LinkedPairId)
                {
                    continue;
                }

                groupSlotIndexes.Add(slot.Index);
            }

            if (groupSlotIndexes.Count != GetLinkedGroupSize(pot.LinkedPairId))
            {
                slotIndexes = Array.Empty<int>();
                return false;
            }

            groupSlotIndexes.Sort();
            slotIndexes = groupSlotIndexes;
            return true;
        }

        private int GetLinkedGroupSize(int linkedGroupId)
        {
            return _linkedGroupSizes.TryGetValue(linkedGroupId, out var count) ? count : 0;
        }

        private bool IsCompleteLinkedGroup(PotModel pot, IReadOnlyList<PotModel> linkedPots)
        {
            return pot != null &&
                pot.IsLinked &&
                linkedPots != null &&
                linkedPots.Count == GetLinkedGroupSize(pot.LinkedPairId);
        }

        private static bool AreAllPotsFull(IReadOnlyList<PotModel> pots)
        {
            if (pots == null || pots.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < pots.Count; i++)
            {
                if (!pots[i].IsFull)
                {
                    return false;
                }
            }

            return true;
        }

        private static void Shuffle<T>(IList<T> values, IRandomNumberGenerator random)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var swapIndex = random.Next(0, i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
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
