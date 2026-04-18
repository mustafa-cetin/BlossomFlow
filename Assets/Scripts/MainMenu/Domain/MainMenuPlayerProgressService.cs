using System;
using BlossomFlow.Application.Events;

namespace BlossomFlow.MainMenu.Domain
{
    public sealed class MainMenuPlayerProgressService : IMainMenuPlayerProgressService
    {
        private readonly IEventPublisher _eventPublisher;
        private MainMenuPlayerProgress _current = MainMenuPlayerProgress.Default;

        public MainMenuPlayerProgressService(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        public MainMenuPlayerProgress Current => _current;

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetProgress(new MainMenuPlayerProgress(
                _current.LevelNumber,
                _current.CurrentLives,
                _current.MaxLives,
                _current.Gold + amount,
                _current.NextLifeCountdown));
        }

        public void AddGoldAndAdvanceLevel(int goldAmount)
        {
            SetProgress(new MainMenuPlayerProgress(
                _current.LevelNumber + 1,
                _current.CurrentLives,
                _current.MaxLives,
                _current.Gold + Math.Max(0, goldAmount),
                _current.NextLifeCountdown));
        }

        public void SetProgress(MainMenuPlayerProgress progress)
        {
            _current = progress;
            _eventPublisher.Publish(new MainMenuPlayerProgressChanged(_current));
        }
    }
}
