using System;

namespace BlossomFlow.Gameplay.Domain.Boosters
{
    public sealed class ShuffleBoosterService : IShuffleBoosterService
    {
        public const int DefaultUsesPerLevel = 5;

        private readonly IRandomNumberGenerator _random;

        public ShuffleBoosterService(IRandomNumberGenerator random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            ResetForLevel();
        }

        public int RemainingUses { get; private set; }

        public void ResetForLevel()
        {
            RemainingUses = DefaultUsesPerLevel;
        }

        public bool TryUse(LevelSession session)
        {
            if (RemainingUses <= 0 ||
                session == null ||
                session.State != LevelPlayState.Playing ||
                !session.TryShuffleLaunchQueues(_random))
            {
                return false;
            }

            RemainingUses--;
            return true;
        }
    }
}
