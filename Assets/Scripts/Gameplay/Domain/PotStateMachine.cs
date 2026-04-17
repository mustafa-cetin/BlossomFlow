using System;

namespace BlossomFlow.Gameplay.Domain
{
    public static class PotStateMachine
    {
        public static bool CanTransition(PotState from, PotState to)
        {
            return from switch
            {
                PotState.Idle => to == PotState.Launching,
                PotState.Launching => to == PotState.Conveying || to == PotState.Waiting || to == PotState.Broken,
                PotState.Conveying => to == PotState.Collecting || to == PotState.Waiting || to == PotState.Full || to == PotState.Broken,
                PotState.Collecting => to == PotState.Conveying || to == PotState.Full || to == PotState.Broken,
                PotState.Waiting => to == PotState.Launching || to == PotState.Broken,
                PotState.Full => false,
                PotState.Broken => false,
                _ => false
            };
        }

        public static bool IsTerminal(PotState state)
        {
            return state == PotState.Full || state == PotState.Broken;
        }

        public static void EnsureCanTransition(PotState from, PotState to)
        {
            if (!CanTransition(from, to))
            {
                throw new InvalidOperationException($"Cannot transition pot from {from} to {to}.");
            }
        }
    }
}
