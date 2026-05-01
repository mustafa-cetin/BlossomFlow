using BlossomFlow.Application.Events;
using BlossomFlow.Application.Levels;
using BlossomFlow.Application.SceneLoading;
using BlossomFlow.Application.Ads;
using BlossomFlow.Application.Analytics;
using BlossomFlow.Application.Audio;
using BlossomFlow.Application.Platform.CrazyGames;
using BlossomFlow.Application.Performance;
using BlossomFlow.Application.Tutorial;
using BlossomFlow.MainMenu.Domain;
using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using Resolution = Reflex.Enums.Resolution;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using BlossomFlow.Application.Debugging;
#endif

public class RootInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private string startupTargetSceneName = "Assets/Scenes/GameScene.unity";
    [SerializeField] private string startupTutorialCompletedTargetSceneName = "Assets/Scenes/GameScene.unity";
    [SerializeField] private LevelCatalogAsset levelCatalog;
    [SerializeField] private SoundCatalogAsset soundCatalog;
    [SerializeField, Range(0f, 1f)] private float soundEffectsVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField] private bool enableCrazyGamesSdk = true;
    [SerializeField] private bool enableByteBrewAnalytics = true;
    [SerializeField] private bool playBackgroundMusicOnStartup = true;
    [SerializeField] private MusicTrackId startupMusicTrack = MusicTrackId.Background;
    [SerializeField, Min(1)] private int legacyOvershootReturnLevelNumber = 51;

    public void InstallBindings(ContainerBuilder builder)
    {
        CrazyGamesSdkRuntime.Configure(enableCrazyGamesSdk);

        builder.RegisterValue(
            new CrazyGamesSdkConfiguration(enableCrazyGamesSdk),
            new[] { typeof(CrazyGamesSdkConfiguration) });

        builder.RegisterValue(
            new ByteBrewAnalyticsConfiguration(enableByteBrewAnalytics),
            new[] { typeof(ByteBrewAnalyticsConfiguration) });

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
            typeof(CrazyGamesAudioMuteProvider),
            new[] { typeof(IExternalAudioMuteProvider) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(CrazyGamesGameplaySessionReporter),
            new[]
            {
                typeof(CrazyGamesGameplaySessionReporter),
                typeof(IGameplayPlatformSessionReporter)
            },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterFactory(
            container => new AudioSettingsService(
                container.Resolve<IAudioPlayer>(),
                container.Resolve<IMusicPlayer>(),
                container.Resolve<IExternalAudioMuteProvider>()),
            new[] { typeof(IAudioSettingsService) },
            Lifetime.Singleton,
            Resolution.Eager);

        builder.RegisterType(
            typeof(SoundEventController),
            new[] { typeof(SoundEventController) },
            Lifetime.Singleton,
            Resolution.Eager);

        builder.RegisterType(
            typeof(ByteBrewAnalyticsEventService),
            new[]
            {
                typeof(ByteBrewAnalyticsEventService),
                typeof(IAnalyticsEventService)
            },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(AnalyticsEventDispatcher),
            new[] { typeof(AnalyticsEventDispatcher) },
            Lifetime.Singleton,
            Resolution.Eager);

        builder.RegisterType(
            typeof(PlayerRetentionAnalyticsReporter),
            new[] { typeof(PlayerRetentionAnalyticsReporter) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterFactory(
            container => new BackgroundMusicController(
                container.Resolve<IMusicPlayer>(),
                playBackgroundMusicOnStartup,
                startupMusicTrack),
            new[] { typeof(BackgroundMusicController) },
            Lifetime.Singleton,
            Resolution.Eager);

        builder.RegisterType(
            typeof(UnitySceneLoader),
            new[] { typeof(ISceneLoader) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterFactory(
            container => new SceneTransitionService(container.Resolve<ISceneLoader>()),
            new[] { typeof(ISceneTransitionService) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterFactory(
            container => new LoadingSceneTargetProvider(
                startupTargetSceneName,
                startupTutorialCompletedTargetSceneName,
                container.Resolve<IFirstLevelTutorialProgressService>()),
            new[] { typeof(ILoadingSceneTargetProvider) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterFactory(
            _ => new LevelCatalog(levelCatalog),
            new[]
            {
                typeof(ILevelCatalog),
            },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(LocalLevelCatalogPreloader),
            new[] { typeof(IRemoteLevelCatalogPreloader) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterFactory(
            container => new LevelSelectionService(
                container.Resolve<ILevelCatalog>(),
                container.Resolve<IMainMenuPlayerProgressService>()),
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
            typeof(ByteBrewStartupServiceInitializer),
            new[] { typeof(ByteBrewStartupServiceInitializer) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(MobileNativeRefreshRateConfigurator),
            new[] { typeof(IFrameRateConfigurator) },
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

        builder.RegisterFactory(
            container => new MainMenuPlayerProgressService(
                container.Resolve<IEventPublisher>(),
                container.Resolve<ILevelCatalog>(),
                legacyOvershootReturnLevelNumber),
            new[] { typeof(IMainMenuPlayerProgressService) },
            Lifetime.Singleton,
            Resolution.Eager);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        RegisterRuntimeDebugging(builder);
#endif

        builder.RegisterType(
            typeof(CrazyGamesRewardedAdService),
            new[] { typeof(IRewardedAdService) },
            Lifetime.Singleton,
            Resolution.Lazy);

        Debug.Log("RootInstaller: InstallBindings called");
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static void RegisterRuntimeDebugging(ContainerBuilder builder)
    {
        builder.RegisterType(
            typeof(GameDebugStateService),
            new[] { typeof(IGameDebugStateService) },
            Lifetime.Singleton,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(RuntimeDebugOverlayBootstrapper),
            new[] { typeof(RuntimeDebugOverlayBootstrapper) },
            Lifetime.Singleton,
            Resolution.Eager);
    }
#endif
}
