using System;
using BlossomFlow.Gameplay.Domain.Levels;

namespace BlossomFlow.Application.Levels
{
    public sealed class LevelSelectionService : ILevelSelectionWriter, ICurrentLevelDefinitionProvider
    {
        private readonly ILevelCatalog _levelCatalog;
        private int _currentLevelNumber = 1;

        public LevelSelectionService(ILevelCatalog levelCatalog)
        {
            _levelCatalog = levelCatalog ?? throw new ArgumentNullException(nameof(levelCatalog));
        }

        public int CurrentLevelNumber => _currentLevelNumber;

        public void SelectLevel(int levelNumber)
        {
            _currentLevelNumber = Math.Max(1, levelNumber);
        }

        public bool TryGetCurrentLevelDefinition(out LevelDefinition definition)
        {
            return _levelCatalog.TryGetDefinition(_currentLevelNumber, out definition);
        }
    }
}
