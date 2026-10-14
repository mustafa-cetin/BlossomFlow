using System;
using BlossomFlow.Application.Events;
using BlossomFlow.Application.Levels;

namespace BlossomFlow.Gameplay.Application.Levels
{
    public sealed class LevelAdvanceService : ILevelAdvanceService
    {
        private readonly ILevelSelectionWriter _levelSelectionWriter;
        private readonly IEventPublisher _eventPublisher;

        public LevelAdvanceService(
            ILevelSelectionWriter levelSelectionWriter,
            IEventPublisher eventPublisher)
        {
            _levelSelectionWriter = levelSelectionWriter;
            _eventPublisher = eventPublisher;
        }

        public void AdvanceToLevel(int levelNumber)
        {
            var normalizedLevelNumber = Math.Max(1, levelNumber);
            _levelSelectionWriter.SelectLevel(normalizedLevelNumber);
            _eventPublisher.Publish(new LevelAdvanceRequested(normalizedLevelNumber));
        }
    }
}
