using BlossomFlow.Gameplay.Domain.Levels;

namespace BlossomFlow.Application.Levels
{
    public interface ILevelCatalog
    {
        int LevelCount { get; }
        bool TryGetDefinition(int levelNumber, out LevelDefinition definition);
    }
}
