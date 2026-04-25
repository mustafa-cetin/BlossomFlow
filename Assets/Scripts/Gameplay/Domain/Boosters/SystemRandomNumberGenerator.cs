using System;

namespace BlossomFlow.Gameplay.Domain.Boosters
{
    public sealed class SystemRandomNumberGenerator : IRandomNumberGenerator
    {
        private readonly Random _random = new Random();

        public int Next(int minInclusive, int maxExclusive)
        {
            return _random.Next(minInclusive, maxExclusive);
        }
    }
}
