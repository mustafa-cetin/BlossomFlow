using System;
using System.Collections.Generic;

namespace BlossomFlow.Application.Events
{
    public sealed class EventBus : IEventBus, IDisposable
    {
        private readonly Dictionary<Type, List<Delegate>> _handlersByEventType = new();

        public void Publish<TEvent>(TEvent gameEvent)
        {
            var eventType = typeof(TEvent);
            if (!_handlersByEventType.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
            {
                return;
            }

            var handlersSnapshot = handlers.ToArray();
            for (var i = 0; i < handlersSnapshot.Length; i++)
            {
                if (handlersSnapshot[i] is Action<TEvent> handler)
                {
                    handler.Invoke(gameEvent);
                }
            }
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var eventType = typeof(TEvent);
            if (!_handlersByEventType.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                _handlersByEventType.Add(eventType, handlers);
            }

            handlers.Add(handler);
            return new EventSubscription(() => Unsubscribe(eventType, handler));
        }

        public void Dispose()
        {
            _handlersByEventType.Clear();
        }

        private void Unsubscribe(Type eventType, Delegate handler)
        {
            if (!_handlersByEventType.TryGetValue(eventType, out var handlers))
            {
                return;
            }

            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _handlersByEventType.Remove(eventType);
            }
        }

        private sealed class EventSubscription : IDisposable
        {
            private Action _dispose;

            public EventSubscription(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }
    }
}
