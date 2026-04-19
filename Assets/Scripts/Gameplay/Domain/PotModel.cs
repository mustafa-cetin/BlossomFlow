using System;

namespace BlossomFlow.Gameplay.Domain
{
    public sealed class PotModel
    {
        public event Action<PotModel, PotState, PotState> StateChanged;

        public PotModel(FlowerColor color, int capacity, int linkedPairId = 0)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
            }

            Color = color;
            StartingCapacity = capacity;
            RemainingCapacity = capacity;
            LinkedPairId = Math.Max(0, linkedPairId);
            State = PotState.Idle;
        }

        public FlowerColor Color { get; }
        public int StartingCapacity { get; }
        public int RemainingCapacity { get; private set; }
        public int LinkedPairId { get; }
        public PotState State { get; private set; }
        public bool IsFull => RemainingCapacity == 0;
        public bool IsLinked => LinkedPairId > 0;
        public bool IsTerminal => PotStateMachine.IsTerminal(State);

        public void Launch()
        {
            TransitionTo(PotState.Launching);
        }

        public void BeginConveying()
        {
            TransitionTo(PotState.Conveying);
        }

        public void BeginCollecting()
        {
            TransitionTo(PotState.Collecting);
        }

        public void CollectOne()
        {
            EnsureState(PotState.Collecting);
            if (RemainingCapacity <= 0)
            {
                throw new InvalidOperationException("A full pot cannot collect more flowers.");
            }

            RemainingCapacity--;
            TransitionTo(RemainingCapacity == 0 && !IsLinked ? PotState.Full : PotState.Conveying);
        }

        public void PlaceInWaiting()
        {
            if (IsFull && !IsLinked)
            {
                throw new InvalidOperationException("A full pot should leave play instead of entering a waiting slot.");
            }

            TransitionTo(PotState.Waiting);
        }

        public void Break()
        {
            if (IsFull)
            {
                throw new InvalidOperationException("A full pot cannot break from waiting overflow.");
            }

            TransitionTo(PotState.Broken);
        }

        private void TransitionTo(PotState nextState)
        {
            PotStateMachine.EnsureCanTransition(State, nextState);

            var previousState = State;
            State = nextState;
            StateChanged?.Invoke(this, previousState, nextState);
        }

        private void EnsureState(PotState expectedState)
        {
            if (State != expectedState)
            {
                throw new InvalidOperationException($"Cannot perform this action while pot is {State}; expected {expectedState}.");
            }
        }
    }
}
