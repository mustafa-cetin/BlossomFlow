using System;

namespace BlossomFlow.Application.Events
{
    public interface IEventSubscriber
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
    }
}
