using BlossomFlow.Application.SceneLoading;
using BlossomFlow.MainMenu.DailyLogin;
using BlossomFlow.MainMenu.Domain;
using BlossomFlow.MainMenu.Presentation;
using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using Resolution = Reflex.Enums.Resolution;

public class MainMenuInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private string levelSceneName = "GameScene";

    public void InstallBindings(ContainerBuilder builder)
    {
        builder.RegisterType(
            typeof(MainMenuNavigator),
            new[] { typeof(IMainMenuNavigator) },
            Lifetime.Scoped,
            Resolution.Eager);

        builder.RegisterFactory(
            _ => new MainMenuGardenConfig(),
            new[] { typeof(MainMenuGardenConfig) },
            Lifetime.Scoped,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(MainMenuGardenService),
            new[] { typeof(IGardenService) },
            Lifetime.Scoped,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(DailyLoginBonusService),
            new[] { typeof(IDailyLoginBonusService) },
            Lifetime.Scoped,
            Resolution.Lazy);

        builder.RegisterFactory(
            container => new UnityLevelSceneLoader(levelSceneName, container.Resolve<ISceneTransitionService>()),
            new[] { typeof(ILevelSceneLoader) },
            Lifetime.Scoped,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(MainMenuPlayController),
            new[] { typeof(MainMenuPlayController) },
            Lifetime.Scoped,
            Resolution.Eager);
    }
}
