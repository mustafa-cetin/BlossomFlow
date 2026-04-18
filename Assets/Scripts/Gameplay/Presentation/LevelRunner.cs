using System;
using System.Collections.Generic;
using BlossomFlow.Application.Events;
using BlossomFlow.Gameplay.Domain;
using BlossomFlow.Gameplay.Domain.Levels;
using BlossomFlow.MainMenu.Domain;
using Reflex.Attributes;
using UnityEngine;

namespace BlossomFlow.Gameplay.Presentation
{
    [RequireComponent(typeof(LevelPresentationView))]
    [RequireComponent(typeof(LevelPointerInput))]
    public class LevelRunner : MonoBehaviour
    {
        [Header("Level")]
        [SerializeField] private LevelDefinitionAsset levelAsset;
        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private bool useBuiltInTestLevel = true;

        [Header("Flow")]
        [SerializeField] private float conveyorStepSeconds = 0.45f;
        [SerializeField] private float conveyorEntryDelaySeconds = 0.45f;
        [SerializeField] private LevelPresentationView presentationView;
        [SerializeField] private LevelPointerInput pointerInput;

        private ILevelSessionFactory _sessionFactory;
        private IEventPublisher _eventPublisher;
        private IEventSubscriber _eventSubscriber;
        private IMainMenuPlayerProgressService _progressService;
        private IDisposable _restartRequestedSubscription;
        private LevelSession _session;
        private float _stepTimer;
        private float _conveyorEntryDelayTimer;

        private const float MinimumStepSeconds = 0.01f;

        public LevelSession CurrentSession => _session;

        [Inject]
        protected void Construct(
            ILevelSessionFactory sessionFactory,
            IEventPublisher eventPublisher,
            IEventSubscriber eventSubscriber,
            IMainMenuPlayerProgressService progressService)
        {
            _sessionFactory = sessionFactory;
            _eventPublisher = eventPublisher;
            _eventSubscriber = eventSubscriber;
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
            _restartRequestedSubscription = _eventSubscriber.Subscribe<LevelRestartRequested>(_ => RestartLevel());

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

            UnsubscribeSessionEvents();
            _restartRequestedSubscription?.Dispose();
            _restartRequestedSubscription = null;
        }

        private void Update()
        {
            pointerInput.Tick(_session, target => target.Click());

            if (_session == null || _session.State != LevelPlayState.Playing)
            {
                return;
            }

            if (_conveyorEntryDelayTimer > 0f)
            {
                _conveyorEntryDelayTimer = Mathf.Max(0f, _conveyorEntryDelayTimer - Time.deltaTime);
            }

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
        }

        private LevelDefinition GetLevelDefinition()
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

        private void StartLevelSession()
        {
            UnsubscribeSessionEvents();
            presentationView.UnbindSession();
            presentationView.ClearViews();

            _stepTimer = 0f;
            _conveyorEntryDelayTimer = 0f;

            _session = _sessionFactory.Create(GetLevelDefinition());
            _session.LevelWon += OnLevelWon;
            _session.LevelFailed += OnLevelFailed;

            presentationView.BindSession(_session);
            _session.Start();
            presentationView.BuildInitialViews();
        }

        private void TickActivePots()
        {
            var activePots = new List<PotModel>(_session.ActivePots);
            for (var i = 0; i < activePots.Count && _session.State == LevelPlayState.Playing; i++)
            {
                presentationView.TrackPotentialFlowerTarget(activePots[i]);
                _session.TryCollectAtCurrentConveyorPosition(activePots[i], out _);
            }

            if (_session.State != LevelPlayState.Playing)
            {
                return;
            }

            _session.AdvanceConveyor();
            presentationView.UpdateConveyorPotViews();
        }

        private void TryLaunchColumn(int columnIndex)
        {
            if (!CanEnterConveyor())
            {
                return;
            }

            var pot = _session.LaunchFromColumn(columnIndex);
            StartConveyorEntryDelay();
            presentationView.ShowLaunchedPot(pot, columnIndex);
        }

        private void TryRecallWaitingPot(int slotIndex)
        {
            if (!CanEnterConveyor())
            {
                return;
            }

            if (!_session.WaitingSlots.TryGetPot(slotIndex, out _))
            {
                return;
            }

            var pot = _session.RecallWaitingPot(slotIndex);
            StartConveyorEntryDelay();
            presentationView.ShowRecalledPot(pot, slotIndex);
        }

        private bool CanEnterConveyor()
        {
            return _session != null &&
                _session.State == LevelPlayState.Playing &&
                _session.Conveyor.CanEnter &&
                _conveyorEntryDelayTimer <= 0f;
        }

        private float GetCurrentConveyorStepSeconds()
        {
            return presentationView != null
                ? Mathf.Max(MinimumStepSeconds, presentationView.GetGridScaledDuration(conveyorStepSeconds))
                : conveyorStepSeconds;
        }

        private void StartConveyorEntryDelay()
        {
            _conveyorEntryDelayTimer = conveyorEntryDelaySeconds;
        }

        private void OnLevelWon(LevelSession session)
        {
            presentationView.ShowWinResolution();
            _eventPublisher.Publish(new LevelVictoryScreenRequested());
            Debug.Log("Level won.");
        }

        private void OnLevelFailed(LevelSession session, LevelFailureReason reason, PotModel failedPot)
        {
            _progressService?.TryConsumeLife();
            presentationView.ShowFailResolution(failedPot);
            _eventPublisher.Publish(new LevelFailureScreenRequested());
            Debug.Log($"Level failed: {reason}.");
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

        private void UnsubscribeSessionEvents()
        {
            if (_session == null)
            {
                return;
            }

            _session.LevelWon -= OnLevelWon;
            _session.LevelFailed -= OnLevelFailed;
        }
    }
}
