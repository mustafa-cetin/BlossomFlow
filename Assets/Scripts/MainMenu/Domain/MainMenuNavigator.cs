using BlossomFlow.Application.Events;

namespace BlossomFlow.MainMenu.Domain
{
    public sealed class MainMenuNavigator : IMainMenuNavigator
    {
        private readonly IEventPublisher _eventPublisher;

        public MainMenuNavigator(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
            CurrentTab = MainMenuTab.Home;
        }

        public MainMenuTab CurrentTab { get; private set; }

        public bool TrySelect(MainMenuTab tab)
        {
            if (tab == CurrentTab)
            {
                return false;
            }

            var previousTab = CurrentTab;
            CurrentTab = tab;
            _eventPublisher.Publish(new MainMenuTabChanged(previousTab, CurrentTab));
            return true;
        }

        public bool TryMove(int direction)
        {
            if (direction == 0)
            {
                return false;
            }

            var nextTabIndex = (int)CurrentTab + direction;
            if (nextTabIndex < (int)MainMenuTab.Shop || nextTabIndex > (int)MainMenuTab.Friends)
            {
                return false;
            }

            return TrySelect((MainMenuTab)nextTabIndex);
        }
    }
}
