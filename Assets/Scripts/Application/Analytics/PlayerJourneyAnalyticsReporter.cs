using System;
using System.Collections.Generic;
using BlossomFlow.Application.Events;
using BlossomFlow.Gameplay.Domain.Boosters;
using BlossomFlow.Gameplay.Presentation;
using BlossomFlow.MainMenu.Domain;

namespace BlossomFlow.Application.Analytics
{
    public sealed class PlayerJourneyAnalyticsReporter : IDisposable
    {
        private const string StepAppStarted = "AppStarted";
        private const string StepPlayPressed = "PlayPressed";
        private const string StepLevelStarted = "LevelStarted";
        private const string StepLevelEnded = "LevelEnded";
        private const string StepFailureScreenShown = "FailureScreenShown";
        private const string StepNoLivesShown = "NoLivesShown";
        private const string StepKeepPlayingStarted = "KeepPlayingStarted";
        private const string StepMainMenuTabChanged = "MainMenuTabChanged";
        private const string StepShuffleBoosterUsed = "ShuffleBoosterUsed";
        private const string ShuffleBoosterType = "Shuffle";

        private readonly IEventPublisher _eventPublisher;
        private readonly IMainMenuPlayerProgressService _progressService;
        private readonly IDisposable _analyticsEventRequestedSubscription;
        private readonly IDisposable _mainMenuPlayRequestedSubscription;
        private readonly IDisposable _mainMenuTabChangedSubscription;
        private readonly IDisposable _levelFailureScreenRequestedSubscription;
        private readonly IDisposable _levelKeepPlayingStartedSubscription;
        private readonly IDisposable _shuffleBoosterUsedSubscription;
        private string _lastStep = string.Empty;

        public PlayerJourneyAnalyticsReporter(
            IEventPublisher eventPublisher,
            IEventSubscriber eventSubscriber,
            IMainMenuPlayerProgressService progressService)
        {
            _eventPublisher = eventPublisher;
            _progressService = progressService;
            _analyticsEventRequestedSubscription = eventSubscriber.Subscribe<AnalyticsEventRequested>(OnAnalyticsEventRequested);
            _mainMenuPlayRequestedSubscription = eventSubscriber.Subscribe<MainMenuPlayRequested>(OnMainMenuPlayRequested);
            _mainMenuTabChangedSubscription = eventSubscriber.Subscribe<MainMenuTabChanged>(OnMainMenuTabChanged);
            _levelFailureScreenRequestedSubscription = eventSubscriber.Subscribe<LevelFailureScreenRequested>(OnLevelFailureScreenRequested);
            _levelKeepPlayingStartedSubscription = eventSubscriber.Subscribe<LevelKeepPlayingStarted>(_ => PublishJourneyEvent(
                AnalyticsCustomEventNames.JourneyKeepPlayingStarted,
                StepKeepPlayingStarted));
            _shuffleBoosterUsedSubscription = eventSubscriber.Subscribe<ShuffleBoosterUsed>(OnShuffleBoosterUsed);
        }

        public void Dispose()
        {
            _analyticsEventRequestedSubscription?.Dispose();
            _mainMenuPlayRequestedSubscription?.Dispose();
            _mainMenuTabChangedSubscription?.Dispose();
            _levelFailureScreenRequestedSubscription?.Dispose();
            _levelKeepPlayingStartedSubscription?.Dispose();
            _shuffleBoosterUsedSubscription?.Dispose();
        }

        public void ReportAppStarted()
        {
            PublishJourneyEvent(AnalyticsCustomEventNames.JourneyAppStarted, StepAppStarted);
        }

        private void OnAnalyticsEventRequested(AnalyticsEventRequested analyticsEvent)
        {
            if (!string.Equals(analyticsEvent.EventName, AnalyticsCustomEventNames.Level, StringComparison.Ordinal) ||
                analyticsEvent.Parameters == null ||
                !analyticsEvent.Parameters.TryGetValue(LevelAnalyticsParameterNames.Status, out var status))
            {
                return;
            }

            if (string.Equals(status, "Start", StringComparison.Ordinal))
            {
                PublishJourneyEvent(AnalyticsCustomEventNames.JourneyLevelStarted, StepLevelStarted, analyticsEvent.Parameters);
                return;
            }

            if (string.Equals(status, "Complete", StringComparison.Ordinal) ||
                string.Equals(status, "Failed", StringComparison.Ordinal) ||
                string.Equals(status, "Quit", StringComparison.Ordinal) ||
                string.Equals(status, "Restart", StringComparison.Ordinal))
            {
                PublishJourneyEvent(AnalyticsCustomEventNames.JourneyLevelEnded, StepLevelEnded, analyticsEvent.Parameters);
            }
        }

        private void OnMainMenuPlayRequested(MainMenuPlayRequested playRequested)
        {
            var parameters = CreateProgressParameters()
                .Add(JourneyAnalyticsParameterNames.LevelID, playRequested.LevelNumber);

            PublishJourneyEvent(AnalyticsCustomEventNames.JourneyPlayPressed, StepPlayPressed, parameters.Build());
        }

        private void OnMainMenuTabChanged(MainMenuTabChanged tabChanged)
        {
            var parameters = CreateProgressParameters()
                .Add(JourneyAnalyticsParameterNames.PreviousTab, tabChanged.PreviousTab.ToString())
                .Add(JourneyAnalyticsParameterNames.CurrentTab, tabChanged.CurrentTab.ToString());

            PublishJourneyEvent(AnalyticsCustomEventNames.JourneyMainMenuTabChanged, StepMainMenuTabChanged, parameters.Build());
        }

        private void OnLevelFailureScreenRequested(LevelFailureScreenRequested failureScreenRequested)
        {
            PublishJourneyEvent(
                failureScreenRequested.SkipKeepPlayingOffer
                    ? AnalyticsCustomEventNames.JourneyNoLivesShown
                    : AnalyticsCustomEventNames.JourneyFailureScreenShown,
                failureScreenRequested.SkipKeepPlayingOffer ? StepNoLivesShown : StepFailureScreenShown);
        }

        private void OnShuffleBoosterUsed(ShuffleBoosterUsed boosterUsed)
        {
            var parameters = CreateProgressParameters()
                .Add(JourneyAnalyticsParameterNames.BoosterType, ShuffleBoosterType)
                .Add(JourneyAnalyticsParameterNames.RemainingUses, boosterUsed.RemainingUses);

            PublishJourneyEvent(AnalyticsCustomEventNames.JourneyShuffleBoosterUsed, StepShuffleBoosterUsed, parameters.Build());
        }

        private void PublishJourneyEvent(string eventName, string step)
        {
            PublishJourneyEvent(eventName, step, null);
        }

        private void PublishJourneyEvent(
            string eventName,
            string step,
            IReadOnlyDictionary<string, string> sourceParameters)
        {
            if (_eventPublisher == null)
            {
                return;
            }

            var previousStep = _lastStep;
            _lastStep = step;

            var parameters = CreateProgressParameters();
            CopyLevelParameter(sourceParameters, parameters, LevelAnalyticsParameterNames.LevelID, JourneyAnalyticsParameterNames.LevelID);
            CopyLevelParameter(sourceParameters, parameters, LevelAnalyticsParameterNames.LevelType, JourneyAnalyticsParameterNames.LevelType);
            CopyLevelParameter(sourceParameters, parameters, LevelAnalyticsParameterNames.PlayerLevel, JourneyAnalyticsParameterNames.PlayerLevel);
            CopyLevelParameter(sourceParameters, parameters, LevelAnalyticsParameterNames.Status, JourneyAnalyticsParameterNames.Status);
            CopyLevelParameter(sourceParameters, parameters, LevelAnalyticsParameterNames.AttemptNumber, JourneyAnalyticsParameterNames.AttemptNumber);
            CopyLevelParameter(sourceParameters, parameters, LevelAnalyticsParameterNames.KeepPlayingCount, JourneyAnalyticsParameterNames.KeepPlayingCount);
            CopyLevelParameter(sourceParameters, parameters, LevelAnalyticsParameterNames.DurationSeconds, JourneyAnalyticsParameterNames.DurationSeconds);
            CopyLevelParameter(sourceParameters, parameters, LevelAnalyticsParameterNames.FailReason, JourneyAnalyticsParameterNames.FailReason);
            CopyLevelParameter(sourceParameters, parameters, LevelAnalyticsParameterNames.CurrencyBalanceGold, JourneyAnalyticsParameterNames.CurrencyBalanceGold);
            CopyLevelParameter(sourceParameters, parameters, LevelAnalyticsParameterNames.CurrencyBalanceMagicalSeed, JourneyAnalyticsParameterNames.CurrencyBalanceMagicalSeed);

            parameters
                .Add(JourneyAnalyticsParameterNames.Step, step)
                .Add(JourneyAnalyticsParameterNames.PreviousStep, previousStep);

            _eventPublisher.Publish(new AnalyticsEventRequested(eventName, parameters.Build()));
        }

        private AnalyticsEventParameters CreateProgressParameters()
        {
            var progress = _progressService != null ? _progressService.Current : MainMenuPlayerProgress.Default;
            return new AnalyticsEventParameters()
                .Add(JourneyAnalyticsParameterNames.PlayerLevel, progress.LevelNumber)
                .Add(JourneyAnalyticsParameterNames.CurrencyBalanceGold, progress.Gold)
                .Add(JourneyAnalyticsParameterNames.CurrencyBalanceMagicalSeed, progress.MagicalSeed)
                .Add(JourneyAnalyticsParameterNames.Lives, progress.CurrentLives);
        }

        private static void CopyLevelParameter(
            IReadOnlyDictionary<string, string> sourceParameters,
            AnalyticsEventParameters targetParameters,
            string sourceKey,
            string targetKey)
        {
            if (sourceParameters != null && sourceParameters.TryGetValue(sourceKey, out var value))
            {
                targetParameters.Add(targetKey, value);
            }
        }
    }
}
