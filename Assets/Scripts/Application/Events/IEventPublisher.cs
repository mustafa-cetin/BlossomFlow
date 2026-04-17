namespace BlossomFlow.Application.Events
{
    public interface IEventPublisher
    {
        void Publish<TEvent>(TEvent gameEvent);
    }
}
