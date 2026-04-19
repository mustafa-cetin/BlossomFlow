using System;
using System.Collections.Generic;
using BlossomFlow.Application.Audio;
using BlossomFlow.Application.Events;
using BlossomFlow.Application.Levels;
using BlossomFlow.Application.Tutorial;
using BlossomFlow.Gameplay.Domain;
using BlossomFlow.Gameplay.Domain.Levels;
using BlossomFlow.Gameplay.Presentation.Tutorial;
using CrazyGames;
using Reflex.Attributes;
using UnityEngine;

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
        private IDisposable _restartRequestedSubscription;
        private IDisposable _keepPlayingRequestedSubscription;
        private LevelSession _session;
        private float _stepTimer;
        private float _conveyorEntryDelayTimer;
        private bool _isFastForwardingConveyor;
        private bool _isGameplayReportedActive;

        private const float MinimumStepSeconds = 0.01f;

        public LevelSession CurrentSession => _session;

        [Inject]
        protected void Construct(
            ILevelSessionFactory sessionFactory,
            IEventPublisher eventPublisher,
            IEventSubscriber eventSubscriber,
            ICurrentLevelDefinitionProvider currentLevelProvider,
            IFirstLevelTutorialProgressService firstLevelTutorialProgress)
        {
            _sessionFactory = sessionFactory;
            _eventPublisher = eventPublisher;
            _eventSubscriber = eventSubscriber;
            _currentLevelProvider = currentLevelProvider;
            _firstLevelTutorialProgress = firstLevelTutorialProgress;
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
            _keepPlayingRequestedSubscription = _eventSubscriber.Subscribe<LevelKeepPlayingRequested>(_ => TryKeepPlaying());

            StartLevelSession();
        }

        private void OnDestroy()
        {
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
            _keepPlayingRequestedSubscription?.Dispose();
            _keepPlayingRequestedSubscription = null;
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

        private LevelDefinition GetLevelDefinition()
        {
            if (_currentLevelProvider != null &&
                _currentLevelProvider.TryGetCurrentLevelDefinition(out var selectedDefinition))
            {
                return selectedDefinition;
            }

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

        private void StartLevelSession()
        {
            NotifyGameplayStop();
            UnsubscribeSessionEvents();
            presentationView.UnbindSession();
            presentationView.ClearViews();

            _stepTimer = 0f;
            _conveyorEntryDelayTimer = 0f;
            _isFastForwardingConveyor = false;

            _session = _sessionFactory.Create(GetLevelDefinition());
            _session.LevelWon += OnLevelWon;
            _session.LevelFailed += OnLevelFailed;
            _session.WaitingPotPlaced += OnWaitingPotPlaced;
            _session.WaitingPotRecalled += OnWaitingPotRecalled;
            _session.Conveyor.PotEntered += OnConveyorPotCountChanged;
            _session.Conveyor.PotRemoved += OnConveyorPotCountChanged;

            presentationView.BindSession(_session);
            _session.Start();
            NotifyGameplayStartIfPlaying();
            presentationView.BuildInitialViews();
            firstLevelTutorial.BindSession(_session, _currentLevelProvider?.CurrentLevelNumber ?? 1);
            PublishConveyorPotCount();
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
            if (_session == null || !_session.CanLaunchFromColumn(columnIndex))
            {
                return;
            }

            if (!CanEnterConveyor(_session.GetLaunchGroupSize(columnIndex)))
            {
                PublishConveyorEntryBlockedIfFull();
                return;
            }

            var pots = _session.LaunchGroupFromColumn(columnIndex, out var sourceColumnIndexes);
            StartConveyorEntryDelay();
            PublishSound(SoundCueId.GameplayTap);
            PublishSound(SoundCueId.PotLaunch);
            for (var i = 0; i < pots.Count; i++)
            {
                presentationView.ShowLaunchedPot(pots[i], sourceColumnIndexes[i]);
            }
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
            return _session.WaitingSlots.OccupiedCount + _session.Conveyor.Pots.Count <= _session.WaitingSlots.Capacity;
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
            PublishSound(SoundCueId.LevelWin);
            presentationView.ShowWinResolution();
            _eventPublisher.Publish(new LevelVictoryScreenRequested());
            Debug.Log("Level won.");
        }

        private void OnLevelFailed(LevelSession session, LevelFailureReason reason, PotModel failedPot)
        {
            _isFastForwardingConveyor = false;
            NotifyGameplayStop();
            PublishSound(SoundCueId.LevelFail);
            if (failedPot != null)
            {
                PublishSound(SoundCueId.PotBreak);
            }

            presentationView.ShowFailResolution(failedPot);
            _eventPublisher.Publish(new LevelFailureScreenRequested());
            Debug.Log($"Level failed: {reason}.");
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

            _eventPublisher.Publish(new LevelRestarted());
            StartLevelSession();
        }

        private void TryKeepPlaying()
        {
            if (_session == null || !_session.TryContinueAfterWaitingOverflow())
            {
                return;
            }

            _stepTimer = 0f;
            _conveyorEntryDelayTimer = 0f;
            _isFastForwardingConveyor = false;
            PublishConveyorPotCount();
            NotifyGameplayStartIfPlaying();
            _eventPublisher.Publish(new LevelKeepPlayingStarted());
        }

        private void NotifyGameplayStartIfPlaying()
        {
            if (_session == null || _session.State != LevelPlayState.Playing || _isGameplayReportedActive)
            {
                return;
            }

            if (!CrazySDK.IsAvailable || !CrazySDK.IsInitialized)
            {
                return;
            }

            CrazySDK.Game.GameplayStart();
            _isGameplayReportedActive = true;
        }

        private void NotifyGameplayStop()
        {
            if (!_isGameplayReportedActive)
            {
                return;
            }

            if (CrazySDK.IsAvailable && CrazySDK.IsInitialized)
            {
                CrazySDK.Game.GameplayStop();
            }

            _isGameplayReportedActive = false;
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
