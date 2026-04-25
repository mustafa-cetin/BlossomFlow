using System;
using BlossomFlow.Gameplay.Domain.Levels;
using BlossomFlow.MainMenu.Domain;

namespace BlossomFlow.Application.Levels
{
    public sealed class LevelSelectionService : ILevelSelectionWriter, ICurrentLevelDefinitionProvider
    {
        private readonly ILevelCatalog _levelCatalog;
        private int _currentLevelNumber = 1;

        public LevelSelectionService(ILevelCatalog levelCatalog, IMainMenuPlayerProgressService progressService)
        {
            _levelCatalog = levelCatalog ?? throw new ArgumentNullException(nameof(levelCatalog));
            _currentLevelNumber = Math.Max(1, progressService?.Current.LevelNumber ?? _currentLevelNumber);
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

        public IDisposable LoadCurrentLevelDefinition(Action<LevelDefinition> onLoaded, Action<Exception> onFailed)
        {
            return _levelCatalog.LoadDefinition(_currentLevelNumber, onLoaded, onFailed);
        }
    }
}
