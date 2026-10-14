using System;
using System.Collections.Generic;
using BlossomFlow.Application.Analytics;
using BlossomFlow.Application.Audio;
using BlossomFlow.Application.Events;
using BlossomFlow.Application.Levels;
using BlossomFlow.Application.Platform.CrazyGames;
using BlossomFlow.Application.Tutorial;
using BlossomFlow.Gameplay.Application.Levels;
using BlossomFlow.Gameplay.Domain;
using BlossomFlow.Gameplay.Domain.Boosters;
using BlossomFlow.Gameplay.Domain.Levels;
using BlossomFlow.Gameplay.Presentation.Tutorial;
using BlossomFlow.MainMenu.Domain;
using Reflex.Attributes;
using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using BlossomFlow.Application.Debugging;
#endif

namespace BlossomFlow.Gameplay.Presentation
{
    [RequireComponent(typeof(LevelPresentationView))]
    [RequireComponent(typeof(LevelPointerInput))]
    [RequireComponent(typeof(FirstLevelTutorialController))]
    public class LevelRunner : MonoBehaviour
    {
        [Header("Level")]
        [SerializeField] private LevelDefinitionAsset levelAsset;
        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private bool useBuiltInTestLevel;

        [Header("Flow")]
        [SerializeField] private float conveyorStepSeconds = 0.45f;
        [SerializeField] private float conveyorEntryDelaySeconds = 0.45f;
        [SerializeField] private bool fastForwardConveyorWhenQueuesEmpty = true;
        [SerializeField] private float queueEmptyConveyorSpeedMultiplier = 3f;
        [SerializeField] private LevelPresentationView presentationView;
        [SerializeField] private LevelPointerInput pointerInput;
        [SerializeField] private FirstLevelTutorialController firstLevelTutorial;

        private ILevelSessionFactory _sessionFactory;
        private IEventPublisher _eventPublisher;
        private IEventSubscriber _eventSubscriber;
        private ICurrentLevelDefinitionProvider _currentLevelProvider;
        private IFirstLevelTutorialProgressService _firstLevelTutorialProgress;
        private IGameplayPlatformSessionReporter _gameplaySessionReporter;
        private IShuffleBoosterService _shuffleBoosterService;
        private IExtraWaitingSlotBoosterService _extraWaitingSlotBoosterService;
        private IMainMenuPlayerProgressService _progressService;
        private IDisposable _restartRequestedSubscription;
        private IDisposable _levelAdvanceRequestedSubscription;
        private IDisposable _keepPlayingRequestedSubscription;
        private IDisposable _shuffleBoosterRequestedSubscription;
        private IDisposable _extraWaitingSlotBoosterRequestedSubscription;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private IDisposable _debugLevelReloadRequestedSubscription;
#endif
        private IDisposable _levelLoadRequest;
        private LevelSession _session;
        private float _stepTimer;
        private float _conveyorEntryDelayTimer;
        private bool _isFastForwardingConveyor;
        private bool _isCurrentLevelResultTracked;
        private float _levelStartedAtSeconds;
        private int _currentLevelAttemptNumber;
        private int _currentLevelAttemptLevelNumber;
        private int _currentLevelKeepPlayingCount;
        private int _lastStartedLevelNumber;
        private int _levelLoadVersion;

        private const float MinimumStepSeconds = 0.01f;
        private const string CampaignLevelType = "Campaign";
        private const string LevelStatusComplete = "Complete";
        private const string LevelStatusFailed = "Failed";
        private const string LevelStatusKeepPlaying = "KeepPlaying";
        private const string LevelStatusQuit = "Quit";
        private const string LevelStatusRestart = "Restart";
        private const string LevelStatusStart = "Start";

        public LevelSession CurrentSession => _session;

        [Inject]
        protected void Construct(
            ILevelSessionFactory sessionFactory,
            IEventPublisher eventPublisher,
            IEventSubscriber eventSubscriber,
            ICurrentLevelDefinitionProvider currentLevelProvider,
            IFirstLevelTutorialProgressService firstLevelTutorialProgress,
            IGameplayPlatformSessionReporter gameplaySessionReporter,
            IShuffleBoosterService shuffleBoosterService,
            IExtraWaitingSlotBoosterService extraWaitingSlotBoosterService,
            IMainMenuPlayerProgressService progressService)
        {
            _sessionFactory = sessionFactory;
            _eventPublisher = eventPublisher;
            _eventSubscriber = eventSubscriber;
            _currentLevelProvider = currentLevelProvider;
            _firstLevelTutorialProgress = firstLevelTutorialProgress;
            _gameplaySessionReporter = gameplaySessionReporter;
            _shuffleBoosterService = shuffleBoosterService;
            _extraWaitingSlotBoosterService = extraWaitingSlotBoosterService;
            _progressService = progressService;
        }

        private void Awake()
        {
            EnsureComponents();
        }

        private void Start()
        {
            if (_sessionFactory == null || _eventPublisher == null || _eventSubscriber == null)
            {
                Debug.LogError("LevelRunner needs Reflex injection. Add a SceneScope with GameInstaller to this scene.");
                enabled = false;
                return;
            }

            presentationView.LaunchColumnRequested += TryLaunchColumn;
            presentationView.WaitingPotRecallRequested += TryRecallWaitingPot;
            firstLevelTutorial.Initialize(presentationView, _firstLevelTutorialProgress);
            _restartRequestedSubscription = _eventSubscriber.Subscribe<LevelRestartRequested>(_ => RestartLevel());
            _levelAdvanceRequestedSubscription = _eventSubscriber.Subscribe<LevelAdvanceRequested>(OnLevelAdvanceRequested);
            _keepPlayingRequestedSubscription = _eventSubscriber.Subscribe<LevelKeepPlayingRequested>(_ => TryKeepPlaying());
            _shuffleBoosterRequestedSubscription = _eventSubscriber.Subscribe<ShuffleBoosterRequested>(_ => TryUseShuffleBooster());
            _extraWaitingSlotBoosterRequestedSubscription = _eventSubscriber.Subscribe<ExtraWaitingSlotBoosterRequested>(_ => TryUseExtraWaitingSlotBooster());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _debugLevelReloadRequestedSubscription = _eventSubscriber.Subscribe<GameDebugLevelReloadRequested>(OnDebugLevelReloadRequested);
#endif

            if (!TryShowNoLivesScreen())
            {
                StartLevelSession();
            }
        }

        private void OnDestroy()
        {
            TrackLevelQuitIfPlaying();

            if (presentationView != null)
            {
                presentationView.LaunchColumnRequested -= TryLaunchColumn;
                presentationView.WaitingPotRecallRequested -= TryRecallWaitingPot;
                presentationView.UnbindSession();
            }

            if (firstLevelTutorial != null)
            {
                firstLevelTutorial.UnbindSession();
            }

            NotifyGameplayStop();
            UnsubscribeSessionEvents();
            _restartRequestedSubscription?.Dispose();
            _restartRequestedSubscription = null;
            _levelAdvanceRequestedSubscription?.Dispose();
            _levelAdvanceRequestedSubscription = null;
            _keepPlayingRequestedSubscription?.Dispose();
            _keepPlayingRequestedSubscription = null;
            _shuffleBoosterRequestedSubscription?.Dispose();
            _shuffleBoosterRequestedSubscription = null;
            _extraWaitingSlotBoosterRequestedSubscription?.Dispose();
            _extraWaitingSlotBoosterRequestedSubscription = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _debugLevelReloadRequestedSubscription?.Dispose();
            _debugLevelReloadRequestedSubscription = null;
#endif
            _levelLoadRequest?.Dispose();
            _levelLoadRequest = null;
        }

        private void Update()
        {
            pointerInput.Tick(_session, OnClickTargetRequested);

            if (_session == null || _session.State != LevelPlayState.Playing)
            {
                return;
            }

            if (_conveyorEntryDelayTimer > 0f)
            {
                _conveyorEntryDelayTimer = Mathf.Max(0f, _conveyorEntryDelayTimer - Time.deltaTime);
            }

            RefreshConveyorFastForwardState();

            _stepTimer += Time.deltaTime;
            if (_stepTimer < GetCurrentConveyorStepSeconds())
            {
                return;
            }

            _stepTimer = 0f;
            TickActivePots();
        }

        private void OnValidate()
        {
            conveyorStepSeconds = Mathf.Max(MinimumStepSeconds, conveyorStepSeconds);
            conveyorEntryDelaySeconds = Mathf.Max(0f, conveyorEntryDelaySeconds);
            queueEmptyConveyorSpeedMultiplier = Mathf.Max(1f, queueEmptyConveyorSpeedMultiplier);
        }

        private void EnsureComponents()
        {
            if (presentationView == null)
            {
                presentationView = GetComponent<LevelPresentationView>();
                if (presentationView == null)
                {
                    presentationView = gameObject.AddComponent<LevelPresentationView>();
                }
            }

            if (pointerInput == null)
            {
                pointerInput = GetComponent<LevelPointerInput>();
                if (pointerInput == null)
                {
                    pointerInput = gameObject.AddComponent<LevelPointerInput>();
                }
            }

            if (firstLevelTutorial == null)
            {
                firstLevelTutorial = GetComponent<FirstLevelTutorialController>();
                if (firstLevelTutorial == null)
                {
                    firstLevelTutorial = gameObject.AddComponent<FirstLevelTutorialController>();
                }
            }
        }

        private void StartLevelSession()
        {
            NotifyGameplayStop();
            UnsubscribeSessionEvents();
            pointerInput?.RequirePointerReleaseBeforeNextClick();
            _session = null;
            _levelLoadRequest?.Dispose();
            _levelLoadRequest = null;
            presentationView.UnbindSession();
            presentationView.ClearViews();

            _stepTimer = 0f;
            _conveyorEntryDelayTimer = 0f;
            _isFastForwardingConveyor = false;

            var loadVersion = ++_levelLoadVersion;
            if (_currentLevelProvider != null)
            {
                _levelLoadRequest = _currentLevelProvider.LoadCurrentLevelDefinition(
                    definition => StartLoadedLevelSession(loadVersion, definition),
                    exception => StartFallbackLevelSession(loadVersion, exception));
                return;
            }

            StartLoadedLevelSession(loadVersion, GetFallbackLevelDefinition());
        }

        private bool TryShowNoLivesScreen()
        {
            if (_progressService == null || _progressService.Current.CurrentLives > 0)
            {
                return false;
            }

            _eventPublisher.Publish(new LevelFailureScreenRequested(true));
            return true;
        }

        private void StartLoadedLevelSession(int loadVersion, LevelDefinition definition)
        {
            if (loadVersion != _levelLoadVersion)
            {
                return;
            }

            _levelLoadRequest?.Dispose();
            _levelLoadRequest = null;

            CreateLevelSession(definition ?? GetFallbackLevelDefinition());
        }

        private void StartFallbackLevelSession(int loadVersion, Exception exception)
        {
            if (loadVersion != _levelLoadVersion)
            {
                return;
            }

            if (exception != null)
            {
                Debug.LogWarning($"Could not load selected level definition. Fallback level will be used. {exception.Message}");
            }

            StartLoadedLevelSession(loadVersion, GetFallbackLevelDefinition());
        }

        private LevelDefinition GetFallbackLevelDefinition()
        {
            if (useBuiltInTestLevel)
            {
                return BuiltInPrototypeLevelDefinition.Create();
            }

            if (levelAsset != null && levelAsset.Definition != null)
            {
                return levelAsset.Definition;
            }

            return levelDefinition ?? BuiltInPrototypeLevelDefinition.Create();
        }

        private void CreateLevelSession(LevelDefinition definition)
        {
            _session = _sessionFactory.Create(definition);
            _session.LevelWon += OnLevelWon;
            _session.LevelFailed += OnLevelFailed;
            _session.WaitingPotPlaced += OnWaitingPotPlaced;
            _session.WaitingPotRecalled += OnWaitingPotRecalled;
            _session.Conveyor.PotEntered += OnConveyorPotCountChanged;
            _session.Conveyor.PotRemoved += OnConveyorPotCountChanged;
            _shuffleBoosterService?.ResetForLevel();
            _extraWaitingSlotBoosterService?.ResetForLevel();
            _extraWaitingSlotBoosterService?.SetUnlockedForLevel(GetCurrentLevelNumber());

            presentationView.BindSession(_session);
            _session.Start();
            TrackLevelStarted();
            NotifyGameplayStartIfPlaying();
            presentationView.BuildInitialViews();
            firstLevelTutorial.BindSession(_session, _currentLevelProvider?.CurrentLevelNumber ?? 1);
            PublishConveyorPotCount();
            PublishShuffleBoosterUsesChanged();
            PublishExtraWaitingSlotBoosterUsesChanged();
        }

        private void OnClickTargetRequested(LevelClickTarget target)
        {
            if (target == null || !firstLevelTutorial.CanClick(target))
            {
                return;
            }

            target.Click();
            firstLevelTutorial.HandleAcceptedClick(target);
        }

        private void TickActivePots()
        {
            var activePots = new List<PotModel>(_session.ActivePots);
            for (var i = 0; i < activePots.Count && _session.State == LevelPlayState.Playing; i++)
            {
                if (!_session.IsActive(activePots[i]))
                {
                    continue;
                }

                var activePot = activePots[i];
                presentationView.TrackPotentialFlowerTarget(activePot);
                if (_session.TryCollectAtCurrentConveyorPosition(activePot, out _))
                {
                    PublishSound(SoundCueId.FlowerCollect);
                    if (activePot.IsFull)
                    {
                        PublishSound(SoundCueId.PotFull);
                    }
                }
            }

            if (_session.State != LevelPlayState.Playing)
            {
                return;
            }

            _session.AdvanceConveyor(IsConveyorFastForwardActive());
            presentationView.UpdateConveyorPotViews(GetConveyorAnimationDurationMultiplier());
        }

        private void TryLaunchColumn(int columnIndex)
        {
            if (_session == null || !TryResolveLaunchColumn(columnIndex, out var launchColumnIndex))
            {
                return;
            }

            if (!CanEnterConveyor(_session.GetLaunchGroupSize(launchColumnIndex)))
            {
                PublishConveyorEntryBlockedIfFull();
                return;
            }

            var pots = _session.LaunchGroupFromColumn(launchColumnIndex, out var sourceColumnIndexes);
            StartConveyorEntryDelay();
            PublishSound(SoundCueId.GameplayTap);
            PublishSound(SoundCueId.PotLaunch);
            for (var i = 0; i < pots.Count; i++)
            {
                presentationView.ShowLaunchedPot(pots[i], sourceColumnIndexes[i]);
            }
        }

        private bool TryResolveLaunchColumn(int requestedColumnIndex, out int resolvedColumnIndex)
        {
            resolvedColumnIndex = -1;
            if (_session == null ||
                requestedColumnIndex < 0 ||
                requestedColumnIndex >= _session.LaunchColumns.Count)
            {
                return false;
            }

            if (_session.CanLaunchFromColumn(requestedColumnIndex))
            {
                resolvedColumnIndex = requestedColumnIndex;
                return true;
            }

            var requestedColumn = _session.LaunchColumns[requestedColumnIndex];
            if (!requestedColumn.CanLaunch)
            {
                return false;
            }

            var requestedPot = requestedColumn.PeekTop();
            if (requestedPot == null || !requestedPot.IsLinked)
            {
                return false;
            }

            for (var i = 0; i < _session.LaunchColumns.Count; i++)
            {
                var candidateColumn = _session.LaunchColumns[i];
                if (!candidateColumn.CanLaunch || !_session.CanLaunchFromColumn(i))
                {
                    continue;
                }

                var candidatePot = candidateColumn.PeekTop();
                if (candidatePot != null &&
                    candidatePot.IsLinked &&
                    candidatePot.LinkedPairId == requestedPot.LinkedPairId)
                {
                    resolvedColumnIndex = i;
                    return true;
                }
            }

            return false;
        }

        private void TryRecallWaitingPot(int slotIndex)
        {
            if (_session == null || !_session.CanRecallWaitingPot(slotIndex))
            {
                return;
            }

            if (!CanEnterConveyor(_session.GetWaitingRecallGroupSize(slotIndex)))
            {
                PublishConveyorEntryBlockedIfFull();
                return;
            }

            if (!_session.WaitingSlots.TryGetPot(slotIndex, out _))
            {
                return;
            }

            RecallWaitingPot(slotIndex, true);
            PublishSound(SoundCueId.GameplayTap);
        }

        private bool CanEnterConveyor(int potCount = 1)
        {
            return _session != null &&
                _session.State == LevelPlayState.Playing &&
                _session.Conveyor.CanEnterCount(potCount) &&
                _conveyorEntryDelayTimer <= 0f;
        }

        private float GetCurrentConveyorStepSeconds()
        {
            var stepSeconds = presentationView != null
                ? Mathf.Max(MinimumStepSeconds, presentationView.GetGridScaledDuration(conveyorStepSeconds))
                : conveyorStepSeconds;

            return IsConveyorFastForwardActive()
                ? Mathf.Max(MinimumStepSeconds, stepSeconds / GetQueueEmptyConveyorSpeedMultiplier())
                : stepSeconds;
        }

        private void StartConveyorEntryDelay()
        {
            _conveyorEntryDelayTimer = conveyorEntryDelaySeconds;
        }

        private void RecallWaitingPot(int slotIndex, bool useEntryDelay)
        {
            if (!_session.WaitingSlots.TryGetPot(slotIndex, out _))
            {
                return;
            }

            var pots = _session.RecallWaitingPotGroup(slotIndex, out var recalledSlotIndexes);

            if (useEntryDelay)
            {
                StartConveyorEntryDelay();
            }

            for (var i = 0; i < pots.Count; i++)
            {
                presentationView.ShowRecalledPot(pots[i], recalledSlotIndexes[i]);
            }
        }

        private void RefreshConveyorFastForwardState()
        {
            _isFastForwardingConveyor = ShouldFastForwardConveyor();
        }

        private bool ShouldFastForwardConveyor()
        {
            return fastForwardConveyorWhenQueuesEmpty &&
                _session != null &&
                _session.State == LevelPlayState.Playing &&
                AreLaunchColumnsEmpty() &&
                _session.Conveyor.Pots.Count > 0 &&
                IsWaitingAndConveyorPotCountAtWaitingCapacity();
        }

        private bool IsWaitingAndConveyorPotCountAtWaitingCapacity()
        {
            return _session.WaitingSlots.OccupiedCount + _session.Conveyor.Pots.Count <= _session.WaitingSlots.UsableCapacity;
        }

        private bool IsConveyorFastForwardActive()
        {
            return _isFastForwardingConveyor &&
                _session != null &&
                _session.State == LevelPlayState.Playing;
        }

        private float GetConveyorAnimationDurationMultiplier()
        {
            return IsConveyorFastForwardActive()
                ? 1f / GetQueueEmptyConveyorSpeedMultiplier()
                : 1f;
        }

        private float GetQueueEmptyConveyorSpeedMultiplier()
        {
            return Mathf.Max(1f, queueEmptyConveyorSpeedMultiplier);
        }

        private bool AreLaunchColumnsEmpty()
        {
            if (_session == null)
            {
                return false;
            }

            for (var i = 0; i < _session.LaunchColumns.Count; i++)
            {
                if (_session.LaunchColumns[i].CanLaunch)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnLevelWon(LevelSession session)
        {
            _isFastForwardingConveyor = false;
            NotifyGameplayStop();
            TrackLevelResult(LevelStatusComplete);
            PublishSound(SoundCueId.LevelWin);
            presentationView.ShowWinResolution();
            _eventPublisher.Publish(new LevelVictoryScreenRequested());
            Debug.Log("Level won.");
        }

        private void OnLevelFailed(LevelSession session, LevelFailureReason reason, PotModel failedPot)
        {
            _isFastForwardingConveyor = false;
            NotifyGameplayStop();
            TrackLevelResult(LevelStatusFailed, reason.ToString());
            PublishSound(SoundCueId.LevelFail);
            if (failedPot != null)
            {
                PublishSound(SoundCueId.PotBreak);
            }

            presentationView.ShowFailResolution(failedPot);
            _eventPublisher.Publish(new LevelFailureScreenRequested());
            Debug.Log($"Level failed: {reason}.");
        }

        private void TrackLevelStarted()
        {
            _lastStartedLevelNumber = GetCurrentLevelNumber();
            if (_currentLevelAttemptLevelNumber != _lastStartedLevelNumber)
            {
                _currentLevelAttemptLevelNumber = _lastStartedLevelNumber;
                _currentLevelAttemptNumber = 0;
            }

            _currentLevelAttemptNumber++;
            _currentLevelKeepPlayingCount = 0;
            _levelStartedAtSeconds = Time.realtimeSinceStartup;
            _isCurrentLevelResultTracked = false;
            TrackLevel(LevelStatusStart);
        }

        private void TrackLevelResult(string status, string failReason = null)
        {
            if (_isCurrentLevelResultTracked)
            {
                return;
            }

            _isCurrentLevelResultTracked = true;
            TrackLevel(status, failReason);
        }

        private void TrackLevel(string status, string failReason = null)
        {
            if (_eventPublisher == null || !ShouldTrackLevelStatus(status))
            {
                return;
            }

            var trackedLevelNumber = GetTrackedLevelNumber();
            var playerLevel = trackedLevelNumber;
            var goldBalance = 0;
            var magicalSeedBalance = 0;
            if (_progressService != null)
            {
                var currentProgress = _progressService.Current;
                playerLevel = currentProgress.LevelNumber;
                goldBalance = currentProgress.Gold;
                magicalSeedBalance = currentProgress.MagicalSeed;
            }

            var parameters = new AnalyticsEventParameters()
                .Add(LevelAnalyticsParameterNames.LevelID, trackedLevelNumber)
                .Add(LevelAnalyticsParameterNames.LevelType, CampaignLevelType)
                .Add(LevelAnalyticsParameterNames.PlayerLevel, playerLevel)
                .Add(LevelAnalyticsParameterNames.CurrencyBalanceGold, goldBalance)
                .Add(LevelAnalyticsParameterNames.CurrencyBalanceMagicalSeed, magicalSeedBalance)
                .Add(LevelAnalyticsParameterNames.Status, status)
                .Add(LevelAnalyticsParameterNames.AttemptNumber, _currentLevelAttemptNumber)
                .Add(LevelAnalyticsParameterNames.KeepPlayingCount, _currentLevelKeepPlayingCount);

            if (!string.Equals(status, LevelStatusStart, StringComparison.Ordinal))
            {
                parameters.Add(LevelAnalyticsParameterNames.DurationSeconds, GetCurrentLevelDurationSeconds());
            }

            if (!string.IsNullOrEmpty(failReason))
            {
                parameters.Add(LevelAnalyticsParameterNames.FailReason, failReason);
            }

            _eventPublisher.Publish(new AnalyticsEventRequested(AnalyticsCustomEventNames.Level, parameters.Build()));
        }

        private static bool ShouldTrackLevelStatus(string status)
        {
            return AnalyticsEventValidator.IsValidName(status);
        }

        private void TrackLevelQuitIfPlaying()
        {
            if (_session == null ||
                _session.State != LevelPlayState.Playing ||
                _isCurrentLevelResultTracked)
            {
                return;
            }

            TrackLevelResult(LevelStatusQuit);
        }

        private void TrackLevelRestartIfStarted()
        {
            if (_session == null || _lastStartedLevelNumber <= 0)
            {
                return;
            }

            TrackLevel(LevelStatusRestart);
        }

        private int GetTrackedLevelNumber()
        {
            return _lastStartedLevelNumber > 0 ? _lastStartedLevelNumber : GetCurrentLevelNumber();
        }

        private int GetCurrentLevelNumber()
        {
            return _currentLevelProvider != null ? _currentLevelProvider.CurrentLevelNumber : 1;
        }

        private int GetCurrentLevelDurationSeconds()
        {
            return Mathf.Max(0, Mathf.RoundToInt(Time.realtimeSinceStartup - _levelStartedAtSeconds));
        }

        private void OnWaitingPotPlaced(LevelSession session, PotModel pot, int slotIndex)
        {
            PublishSound(SoundCueId.WaitingPotPlaced);
        }

        private void OnWaitingPotRecalled(LevelSession session, PotModel pot, int slotIndex)
        {
            PublishSound(SoundCueId.WaitingPotRecalled);
        }

        private void OnConveyorPotCountChanged(ConveyorBelt conveyor, ConveyorPot pot)
        {
            PublishConveyorPotCount(conveyor);
        }

        private void PublishConveyorPotCount()
        {
            if (_session == null)
            {
                return;
            }

            PublishConveyorPotCount(_session.Conveyor);
        }

        private void PublishConveyorPotCount(ConveyorBelt conveyor)
        {
            if (_eventPublisher == null || conveyor == null)
            {
                return;
            }

            _eventPublisher.Publish(new ConveyorPotCountChanged(conveyor.Pots.Count, conveyor.MaxPots));
        }

        private void PublishConveyorEntryBlockedIfFull()
        {
            if (_eventPublisher == null ||
                _session == null ||
                _session.State != LevelPlayState.Playing ||
                _session.Conveyor.Pots.Count < _session.Conveyor.MaxPots)
            {
                return;
            }

            _eventPublisher.Publish(new ConveyorPotEntryBlocked(_session.Conveyor.Pots.Count, _session.Conveyor.MaxPots));
            PublishSound(SoundCueId.ConveyorEntryBlocked);
        }

        public void RestartLevel()
        {
            if (_sessionFactory == null)
            {
                return;
            }

            TrackLevelRestartIfStarted();
            _eventPublisher.Publish(new LevelRestarted());
            StartLevelSession();
        }

        private void OnLevelAdvanceRequested(LevelAdvanceRequested advanceRequested)
        {
            if (_sessionFactory == null)
            {
                return;
            }

            _eventPublisher.Publish(new LevelAdvanced(advanceRequested.LevelNumber));
            StartLevelSession();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnDebugLevelReloadRequested(GameDebugLevelReloadRequested reloadRequested)
        {
            RestartLevel();
        }
#endif

        private void TryKeepPlaying()
        {
            if (_session == null || !_session.TryContinueAfterWaitingOverflow())
            {
                return;
            }

            pointerInput?.RequirePointerReleaseBeforeNextClick();
            _stepTimer = 0f;
            _conveyorEntryDelayTimer = 0f;
            _isFastForwardingConveyor = false;
            PublishConveyorPotCount();
            _currentLevelKeepPlayingCount++;
            TrackLevel(LevelStatusKeepPlaying);
            NotifyGameplayStartIfPlaying();
            _eventPublisher.Publish(new LevelKeepPlayingStarted());
        }

        private void TryUseShuffleBooster()
        {
            if (_session == null || _shuffleBoosterService == null)
            {
                PublishShuffleBoosterUnavailable();
                return;
            }

            if (!_shuffleBoosterService.TryUse(_session))
            {
                PublishShuffleBoosterUnavailable();
                return;
            }

            presentationView.PlayShuffleLaunchQueueAnimation();
            PublishSound(SoundCueId.GameplayTap);
            _eventPublisher?.Publish(new ShuffleBoosterUsed(_shuffleBoosterService.RemainingUses));
            PublishShuffleBoosterUsesChanged();
        }

        private void PublishShuffleBoosterUnavailable()
        {
            var remainingUses = _shuffleBoosterService?.RemainingUses ?? 0;
            _eventPublisher?.Publish(new ShuffleBoosterUnavailable(remainingUses));
            PublishShuffleBoosterUsesChanged();
        }

        private void PublishShuffleBoosterUsesChanged()
        {
            var remainingUses = _shuffleBoosterService?.RemainingUses ?? 0;
            _eventPublisher?.Publish(new ShuffleBoosterUsesChanged(remainingUses));
        }

        private void TryUseExtraWaitingSlotBooster()
        {
            if (_session == null || _extraWaitingSlotBoosterService == null)
            {
                PublishExtraWaitingSlotBoosterUnavailable();
                return;
            }

            if (!_extraWaitingSlotBoosterService.TryUse(_session, out var slotIndex))
            {
                PublishExtraWaitingSlotBoosterUnavailable();
                return;
            }

            PublishSound(SoundCueId.GameplayTap);
            _eventPublisher?.Publish(new ExtraWaitingSlotBoosterUsed(_extraWaitingSlotBoosterService.RemainingUses, slotIndex));
            PublishExtraWaitingSlotBoosterUsesChanged();
        }

        private void PublishExtraWaitingSlotBoosterUnavailable()
        {
            var remainingUses = _extraWaitingSlotBoosterService?.RemainingUses ?? 0;
            _eventPublisher?.Publish(new ExtraWaitingSlotBoosterUnavailable(remainingUses));
            PublishExtraWaitingSlotBoosterUsesChanged();
        }

        private void PublishExtraWaitingSlotBoosterUsesChanged()
        {
            var remainingUses = _extraWaitingSlotBoosterService?.RemainingUses ?? 0;
            _eventPublisher?.Publish(new ExtraWaitingSlotBoosterUsesChanged(remainingUses));
        }

        private void NotifyGameplayStartIfPlaying()
        {
            if (_session == null || _session.State != LevelPlayState.Playing)
            {
                return;
            }

            _gameplaySessionReporter?.NotifyGameplayStart();
        }

        private void NotifyGameplayStop()
        {
            _gameplaySessionReporter?.NotifyGameplayStop();
        }

        private void UnsubscribeSessionEvents()
        {
            if (_session == null)
            {
                return;
            }

            _session.LevelWon -= OnLevelWon;
            _session.LevelFailed -= OnLevelFailed;
            _session.WaitingPotPlaced -= OnWaitingPotPlaced;
            _session.WaitingPotRecalled -= OnWaitingPotRecalled;
            _session.Conveyor.PotEntered -= OnConveyorPotCountChanged;
            _session.Conveyor.PotRemoved -= OnConveyorPotCountChanged;
        }

        private void PublishSound(SoundCueId cueId)
        {
            _eventPublisher?.Publish(new SoundRequested(cueId));
        }
    }
}
