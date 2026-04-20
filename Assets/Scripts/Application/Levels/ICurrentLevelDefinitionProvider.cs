using System;
using BlossomFlow.Gameplay.Domain.Levels;

namespace BlossomFlow.Application.Levels
{
    public interface ICurrentLevelDefinitionProvider
    {
        int CurrentLevelNumber { get; }
        bool TryGetCurrentLevelDefinition(out LevelDefinition definition);
        IDisposable LoadCurrentLevelDefinition(Action<LevelDefinition> onLoaded, Action<Exception> onFailed);
    }
}
