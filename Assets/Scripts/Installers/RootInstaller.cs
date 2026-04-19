using BlossomFlow.Application.Events;
using BlossomFlow.Application.Levels;
using BlossomFlow.Application.SceneLoading;
using BlossomFlow.Application.Ads;
using BlossomFlow.Application.Audio;
using BlossomFlow.Application.Sprites;
using BlossomFlow.Application.Tutorial;
using BlossomFlow.MainMenu.Domain;
using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Resolution = Reflex.Enums.Resolution;

public class RootInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private string loadingSceneName = "LoadingScene";
    [SerializeField] private string startupTargetSceneName = "GameScene";
    [SerializeField] private string startupTutorialCompletedTargetSceneName = "MainMenuScene";
    [SerializeField] private LevelCatalogAsset levelCatalog;
    [SerializeField] private AssetReferenceT<LevelCatalogAsset> remoteLevelCatalog;
    [SerializeField] private bool updateAddressableCatalogsBeforeLevelCatalogLoad = true;
    [SerializeField] private SoundCatalogAsset soundCatalog;
    [SerializeField, Range(0f, 1f)] private float soundEffectsVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField] private bool playBackgroundMusicOnStartup = true;
    [SerializeField] private MusicTrackId startupMusicTrack = MusicTrackId.Background;

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
            _ => soundCatalog != null ? (ISoundCatalog)soundCatalog : new NullSoundCatalog(),
            new[] { typeof(ISoundCatalog) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterFactory(
            _ => soundCatalog != null ? (IMusicCatalog)soundCatalog : new NullSoundCatalog(),
            new[] { typeof(IMusicCatalog) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(AddressableSpriteLoader),
            new[] { typeof(IAddressableSpriteLoader) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterFactory(
            container => new UnityAudioPlayer(
                container.Resolve<ISoundCatalog>(),
                container.Resolve<IMusicCatalog>(),
                soundEffectsVolume,
                musicVolume),
            new[]
            {
                typeof(IAudioPlayer),
                typeof(IMusicPlayer)
            },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(SoundEventController),
            new[] { typeof(SoundEventController) },
            Lifetime.Singleton,
            Resolution.Eager);

        builder.RegisterFactory(
            container => new BackgroundMusicController(
                container.Resolve<IMusicPlayer>(),
                playBackgroundMusicOnStartup,
                startupMusicTrack),
            new[] { typeof(BackgroundMusicController) },
            Lifetime.Singleton,
            Resolution.Eager);

        builder.RegisterFactory(
            container => new SceneTransitionService(
                loadingSceneName,
                startupTargetSceneName,
                startupTutorialCompletedTargetSceneName,
                container.Resolve<IFirstLevelTutorialProgressService>()),
            new[]
            {
                typeof(ISceneTransitionService),
                typeof(ILoadingSceneTargetProvider)
            },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterFactory(
            _ => new AddressableLevelCatalogAssetLoader(
                remoteLevelCatalog,
                updateAddressableCatalogsBeforeLevelCatalogLoad),
            new[] { typeof(ILevelCatalogAssetLoader) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterFactory(
            container => new RemoteLevelCatalog(
                levelCatalog,
                container.Resolve<ILevelCatalogAssetLoader>()),
            new[]
            {
                typeof(ILevelCatalog),
                typeof(IRemoteLevelCatalogPreloader)
            },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(LevelSelectionService),
            new[]
            {
                typeof(ILevelSelectionWriter),
                typeof(ICurrentLevelDefinitionProvider)
            },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(FirstLevelTutorialProgressService),
            new[] { typeof(IFirstLevelTutorialProgressService) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(GardenTutorialProgressService),
            new[] { typeof(IGardenTutorialProgressService) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(CrazyGamesStartupServiceInitializer),
            new[] { typeof(CrazyGamesStartupServiceInitializer) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(StartupServiceInitializer),
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
