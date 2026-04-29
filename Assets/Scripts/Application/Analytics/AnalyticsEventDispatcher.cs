using System;
using BlossomFlow.Application.Events;

namespace BlossomFlow.Application.Analytics
{
    public sealed class AnalyticsEventDispatcher : IDisposable
    {
        private readonly IAnalyticsEventService _analyticsEventService;
        private readonly IDisposable _analyticsEventRequestedSubscription;

        public AnalyticsEventDispatcher(
            IEventSubscriber eventSubscriber,
            IAnalyticsEventService analyticsEventService)
        {
            _analyticsEventService = analyticsEventService;
            _analyticsEventRequestedSubscription = eventSubscriber.Subscribe<AnalyticsEventRequested>(Track);
        }

        public void Dispose()
        {
            _analyticsEventRequestedSubscription?.Dispose();
        }

        private void Track(AnalyticsEventRequested analyticsEvent)
        {
            if (analyticsEvent.Parameters == null || analyticsEvent.Parameters.Count == 0)
            {
                _analyticsEventService.TrackCustomEvent(analyticsEvent.EventName);
                return;
            }

            _analyticsEventService.TrackCustomEvent(analyticsEvent.EventName, analyticsEvent.Parameters);
        }
    }
}
