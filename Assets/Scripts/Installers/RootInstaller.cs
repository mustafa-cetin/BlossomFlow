using BlossomFlow.Application.Events;
using BlossomFlow.Application.SceneLoading;
using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using Resolution = Reflex.Enums.Resolution;

public class RootInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private string loadingSceneName = "LoadingScene";

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

        builder.RegisterFactory(
            _ => new SceneTransitionService(loadingSceneName),
            new[]
            {
                typeof(ISceneTransitionService),
                typeof(ILoadingSceneTargetProvider)
            },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(UnityLoadingSceneController),
            new[] { typeof(ILoadingSceneController) },
            Lifetime.Singleton,
            Resolution.Lazy);

        Debug.Log("RootInstaller: InstallBindings called");
    }
}
