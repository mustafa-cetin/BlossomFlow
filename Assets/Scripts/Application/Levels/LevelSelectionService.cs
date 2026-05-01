using System;
using BlossomFlow.Gameplay.Domain.Levels;
using BlossomFlow.MainMenu.Domain;

namespace BlossomFlow.Application.Levels
{
    public sealed class LevelSelectionService : ILevelSelectionWriter, ICurrentLevelDefinitionProvider
    {
        private readonly ILevelCatalog _levelCatalog;
        private readonly Random _random = new Random();
        private int _currentLevelNumber = 1;
        private int _overflowLevelDefinitionNumber;

        public LevelSelectionService(ILevelCatalog levelCatalog, IMainMenuPlayerProgressService progressService)
        {
            _levelCatalog = levelCatalog ?? throw new ArgumentNullException(nameof(levelCatalog));
            _currentLevelNumber = ClampLevelNumber(progressService?.Current.LevelNumber ?? _currentLevelNumber);
        }

        public int CurrentLevelNumber => _currentLevelNumber;

        public void SelectLevel(int levelNumber)
        {
            var clampedLevelNumber = ClampLevelNumber(levelNumber);
            _currentLevelNumber = clampedLevelNumber;
            _overflowLevelDefinitionNumber = 0;
        }

        public bool TryGetCurrentLevelDefinition(out LevelDefinition definition)
        {
            return _levelCatalog.TryGetDefinition(GetCurrentDefinitionLevelNumber(), out definition);
        }

        public IDisposable LoadCurrentLevelDefinition(Action<LevelDefinition> onLoaded, Action<Exception> onFailed)
        {
            return _levelCatalog.LoadDefinition(GetCurrentDefinitionLevelNumber(), onLoaded, onFailed);
        }

        private int ClampLevelNumber(int levelNumber)
        {
            var minLevelNumber = 1;
            var maxLevelNumber = _levelCatalog.LevelCount > 0
                ? _levelCatalog.LevelCount + 1
                : int.MaxValue;

            return Math.Min(maxLevelNumber, Math.Max(minLevelNumber, levelNumber));
        }

        private int GetCurrentDefinitionLevelNumber()
        {
            var levelCount = _levelCatalog.LevelCount;
            if (levelCount <= 0 || _currentLevelNumber <= levelCount)
            {
                return _currentLevelNumber;
            }

            if (_overflowLevelDefinitionNumber <= 0 || _overflowLevelDefinitionNumber > levelCount)
            {
                _overflowLevelDefinitionNumber = _random.Next(1, levelCount + 1);
            }

            return _overflowLevelDefinitionNumber;
        }
    }
}
