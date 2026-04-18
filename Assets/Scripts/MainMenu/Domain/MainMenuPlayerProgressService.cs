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

        public void SetProgress(MainMenuPlayerProgress progress)
        {
            _current = progress;
            _eventPublisher.Publish(new MainMenuPlayerProgressChanged(_current));
        }
    }
}
