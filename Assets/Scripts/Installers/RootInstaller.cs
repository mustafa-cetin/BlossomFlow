using BlossomFlow.Application.Events;
using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using Resolution = Reflex.Enums.Resolution;

public class RootInstaller : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.RegisterType(
            typeof(EventBus),
            new[]
            {
                typeof(IEventBus),
                typeof(IEventPublisher),
                typeof(IEventSubscriber)
            },
            Lifetime.Singleton,
            Resolution.Lazy);
    }
}
