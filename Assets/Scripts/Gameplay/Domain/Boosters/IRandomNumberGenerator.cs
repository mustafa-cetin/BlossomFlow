namespace BlossomFlow.Gameplay.Domain.Boosters
{
    public interface IRandomNumberGenerator
    {
        int Next(int minInclusive, int maxExclusive);
    }
}
