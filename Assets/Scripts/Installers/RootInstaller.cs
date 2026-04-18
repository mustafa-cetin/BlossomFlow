using BlossomFlow.Application.Events;
using BlossomFlow.Application.SceneLoading;
using BlossomFlow.Application.Ads;
using BlossomFlow.MainMenu.Domain;
using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using Resolution = Reflex.Enums.Resolution;

public class RootInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private string loadingSceneName = "LoadingScene";
    [SerializeField] private string startupTargetSceneName = "MainMenuScene";

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
            _ => new SceneTransitionService(loadingSceneName, startupTargetSceneName),
            new[]
            {
                typeof(ISceneTransitionService),
                typeof(ILoadingSceneTargetProvider)
            },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(CrazyGamesStartupServiceInitializer),
            new[] { typeof(IStartupServiceInitializer) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(UnityLoadingSceneController),
            new[] { typeof(ILoadingSceneController) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(MainMenuPlayerProgressService),
            new[] { typeof(IMainMenuPlayerProgressService) },
            Lifetime.Singleton,
            Resolution.Eager);

        builder.RegisterType(
            typeof(CrazyGamesRewardedAdService),
            new[] { typeof(IRewardedAdService) },
            Lifetime.Singleton,
            Resolution.Lazy);

        Debug.Log("RootInstaller: InstallBindings called");
    }
}
