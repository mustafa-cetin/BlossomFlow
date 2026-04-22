using System.Collections.Generic;
using BlossomFlow.Application.Tutorial;
using BlossomFlow.Gameplay.Domain;
using UnityEngine;

namespace BlossomFlow.Gameplay.Presentation.Tutorial
{
    [RequireComponent(typeof(FirstLevelTutorialView))]
    public sealed class FirstLevelTutorialController : MonoBehaviour
    {
        [SerializeField] private int tutorialLevelNumber = 1;
        [SerializeField] private string launchPrompt = "Pota bas";
        [SerializeField] private string queueLaunchPrompt = "Siradaki pota bas";
        [SerializeField] private string waitingPrompt = "Bekleme slotundaki pota bas";
        [SerializeField] private string finalLaunchPrompt = "Kalan iki pota bas";
        [SerializeField] private float nextLaunchPromptDelaySeconds = 0.5f;
        [SerializeField] private Vector3 launchTargetOffset = new Vector3(0f, 0.18f, 0f);
        [SerializeField] private Vector3 waitingTargetOffset = new Vector3(0f, 0.42f, 0f);
        [SerializeField] private Vector3 finalLaunchTargetOffset = new Vector3(0f, 0.18f, 0f);
        [SerializeField] private int maxSimultaneousLaunchHands = 3;

        private IFirstLevelTutorialProgressService _progressService;
        private FirstLevelTutorialView _view;
        private LevelPresentationView _presentationView;
        private LevelSession _session;
        private TutorialStep _step;
        private int _launchTapCount;
        private int _remainingFinalLaunchTapCount;
        private readonly List<int> _guidedLaunchColumns = new();
        private readonly List<int> _guidedWaitingSlots = new();

        public void Initialize(LevelPresentationView presentationView, IFirstLevelTutorialProgressService progressService)
        {
            _presentationView = presentationView;
            _progressService = progressService;
            _view = GetComponent<FirstLevelTutorialView>();
            if (_view == null)
            {
                _view = gameObject.AddComponent<FirstLevelTutorialView>();
            }

            HideVisual();
        }

        public void BindSession(LevelSession session, int levelNumber)
        {
            UnbindSession();
            _session = session;

            if (_session == null ||
                levelNumber != tutorialLevelNumber ||
                _progressService == null ||
                _progressService.IsCompleted)
            {
                Hide();
                return;
            }

            _progressService.MarkCompleted();
            _session.WaitingPotPlaced += OnWaitingPotPlaced;
            _session.WaitingPotRecalled += OnWaitingPotRecalled;
            ShowLaunchPrompt();
        }

        public bool CanClick(LevelClickTarget target)
        {
            if (_step == TutorialStep.Inactive || _step == TutorialStep.Completed)
            {
                return true;
            }

            if (target == null)
            {
                return false;
            }

            return _step switch
            {
                TutorialStep.AwaitingFirstLaunchTap => IsGuidedLaunchColumn(target),
                TutorialStep.AwaitingQueueLaunchTap => IsGuidedLaunchColumn(target),
                TutorialStep.WaitingForPotToPark => true,
                TutorialStep.AwaitingWaitingTap => true,
                TutorialStep.AwaitingFinalLaunchTaps => true,
                _ => false
            };
        }

        public void HandleAcceptedClick(LevelClickTarget target)
        {
            if (target == null)
            {
                return;
            }

            if (_step == TutorialStep.AwaitingFirstLaunchTap)
            {
                if (!IsGuidedLaunchColumn(target))
                {
                    return;
                }

                _launchTapCount = 1;
                _step = TutorialStep.WaitingForQueueLaunchTarget;
                HideVisual();
                StartCoroutine(PointAtQueueLaunchNextFrame());
                return;
            }

            if (_step == TutorialStep.AwaitingQueueLaunchTap)
            {
                if (!IsGuidedLaunchColumn(target))
                {
                    return;
                }

                _launchTapCount = 2;
                HideVisual();

                if (!TryShowExistingWaitingSlot())
                {
                    _step = TutorialStep.WaitingForPotToPark;
                }

                return;
            }

            if (_step == TutorialStep.AwaitingFinalLaunchTaps && IsGuidedFinalLaunchColumn(target))
            {
                _remainingFinalLaunchTapCount--;
                if (_remainingFinalLaunchTapCount <= 0)
                {
                    CompleteTutorial();
                    return;
                }

                if (!ShowFinalLaunchPrompt())
                {
                    CompleteTutorial();
                }
            }
        }

        public void UnbindSession()
        {
            if (_session != null)
            {
                _session.WaitingPotPlaced -= OnWaitingPotPlaced;
                _session.WaitingPotRecalled -= OnWaitingPotRecalled;
            }

            _session = null;
            _launchTapCount = 0;
            _remainingFinalLaunchTapCount = 0;
            _guidedLaunchColumns.Clear();
            _guidedWaitingSlots.Clear();
            Hide();
        }

        private void OnDestroy()
        {
            UnbindSession();
        }

        private void ShowLaunchPrompt()
        {
            _guidedLaunchColumns.Clear();

            if (_presentationView == null)
            {
                if (_launchTapCount > 0)
                {
                    _step = TutorialStep.WaitingForPotToPark;
                    HideVisual();
                    return;
                }

                CompleteTutorial();
                return;
            }

            var positions = new List<Vector3>();
            for (var i = 0; i < _session.LaunchColumns.Count; i++)
            {
                if (!_session.CanLaunchFromColumn(i) ||
                    !_presentationView.TryGetLaunchColumnAnchor(i, out var position))
                {
                    continue;
                }

                _guidedLaunchColumns.Add(i);
                positions.Add(position + launchTargetOffset);

                if (_guidedLaunchColumns.Count >= maxSimultaneousLaunchHands)
                {
                    break;
                }
            }

            if (_guidedLaunchColumns.Count == 0)
            {
                if (_launchTapCount > 0)
                {
                    _step = TutorialStep.WaitingForPotToPark;
                    HideVisual();
                    return;
                }

                CompleteTutorial();
                return;
            }

            _step = _launchTapCount == 0
                ? TutorialStep.AwaitingFirstLaunchTap
                : TutorialStep.AwaitingQueueLaunchTap;

            var prompt = _launchTapCount == 0 ? launchPrompt : queueLaunchPrompt;
            _view.PointAt(positions, prompt);
        }

        private System.Collections.IEnumerator PointAtQueueLaunchNextFrame()
        {
            if (nextLaunchPromptDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(nextLaunchPromptDelaySeconds);
            }
            else
            {
                yield return null;
            }

            if (_step != TutorialStep.WaitingForQueueLaunchTarget)
            {
                yield break;
            }

            ShowLaunchPrompt();
        }

        private bool TryShowExistingWaitingSlot()
        {
            if (_session == null)
            {
                return false;
            }

            _guidedWaitingSlots.Clear();
            var firstOccupiedSlot = -1;

            for (var i = 0; i < _session.WaitingSlots.Slots.Count; i++)
            {
                var slot = _session.WaitingSlots.Slots[i];
                if (slot == null || !slot.IsOccupied)
                {
                    continue;
                }

                _guidedWaitingSlots.Add(slot.Index);
                if (firstOccupiedSlot < 0)
                {
                    firstOccupiedSlot = slot.Index;
                }
            }

            if (firstOccupiedSlot < 0)
            {
                return false;
            }

            ShowWaitingPrompt(firstOccupiedSlot);
            return true;
        }

        private void ShowWaitingPrompt(int slotIndex)
        {
            _step = TutorialStep.AwaitingWaitingTap;
            StartCoroutine(PointAtWaitingSlotNextFrame(slotIndex));
        }

        private void OnWaitingPotPlaced(LevelSession session, PotModel pot, int slotIndex)
        {
            if ((_step != TutorialStep.WaitingForPotToPark && _step != TutorialStep.AwaitingQueueLaunchTap) ||
                slotIndex < 0)
            {
                return;
            }

            if (_step == TutorialStep.AwaitingQueueLaunchTap && _launchTapCount < 2)
            {
                return;
            }

            if (!_guidedWaitingSlots.Contains(slotIndex))
            {
                _guidedWaitingSlots.Add(slotIndex);
            }

            ShowWaitingPrompt(slotIndex);
        }

        private System.Collections.IEnumerator PointAtWaitingSlotNextFrame(int slotIndex)
        {
            yield return null;

            if (_step != TutorialStep.AwaitingWaitingTap || _presentationView == null)
            {
                yield break;
            }

            var positions = new List<Vector3>();
            Vector3? labelAnchor = null;

            for (var i = 0; i < _guidedWaitingSlots.Count; i++)
            {
                var guidedSlot = _guidedWaitingSlots[i];
                if (!_presentationView.TryGetWaitingPotAnchor(guidedSlot, out var position))
                {
                    continue;
                }

                positions.Add(position + waitingTargetOffset);
                if (guidedSlot == slotIndex)
                {
                    labelAnchor = position + waitingTargetOffset;
                }
            }

            AppendLaunchGuidancePositions(positions, launchTargetOffset);

            if (positions.Count == 0)
            {
                yield break;
            }

            _view.PointAt(positions, labelAnchor, waitingPrompt);
        }

        private void OnWaitingPotRecalled(LevelSession session, PotModel pot, int slotIndex)
        {
            if (_step != TutorialStep.AwaitingWaitingTap || !_guidedWaitingSlots.Remove(slotIndex))
            {
                return;
            }

            HideVisual();
            StartCoroutine(PointAtFinalLaunchTargetsNextFrame());
        }

        private System.Collections.IEnumerator PointAtFinalLaunchTargetsNextFrame()
        {
            yield return null;

            if (_step != TutorialStep.AwaitingWaitingTap)
            {
                yield break;
            }

            if (!ShowFinalLaunchPrompt())
            {
                CompleteTutorial();
            }
        }

        private bool ShowFinalLaunchPrompt()
        {
            var positions = new List<Vector3>();

            if (_session == null || _presentationView == null)
            {
                return false;
            }

            AppendLaunchGuidancePositions(positions, finalLaunchTargetOffset);
            if (positions.Count == 0)
            {
                return false;
            }

            if (_step != TutorialStep.AwaitingFinalLaunchTaps)
            {
                _remainingFinalLaunchTapCount = 2;
            }

            _step = TutorialStep.AwaitingFinalLaunchTaps;
            _view.PointAt(positions, finalLaunchPrompt);
            return true;
        }

        private bool IsGuidedFinalLaunchColumn(LevelClickTarget target)
        {
            return target != null &&
                target.Kind == LevelClickTargetKind.LaunchColumn &&
                IsValidLaunchColumnIndex(target.Index);
        }

        private bool IsGuidedLaunchColumn(LevelClickTarget target)
        {
            return target != null &&
                target.Kind == LevelClickTargetKind.LaunchColumn &&
                ContainsEquivalentGuidedLaunchColumn(target.Index);
        }

        private bool ContainsEquivalentGuidedLaunchColumn(int columnIndex)
        {
            for (var i = 0; i < _guidedLaunchColumns.Count; i++)
            {
                if (AreEquivalentLaunchColumns(columnIndex, _guidedLaunchColumns[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void AppendLaunchGuidancePositions(List<Vector3> positions, Vector3 offset)
        {
            if (_session == null || _presentationView == null)
            {
                return;
            }

            for (var i = 0; i < _session.LaunchColumns.Count; i++)
            {
                if (!_session.CanLaunchFromColumn(i) ||
                    !_presentationView.TryGetLaunchColumnAnchor(i, out var position))
                {
                    continue;
                }

                positions.Add(position + offset);
            }
        }

        private bool AreEquivalentLaunchColumns(int firstColumnIndex, int secondColumnIndex)
        {
            if (firstColumnIndex == secondColumnIndex)
            {
                return firstColumnIndex >= 0;
            }

            if (_session == null ||
                !IsValidLaunchColumnIndex(firstColumnIndex) ||
                !IsValidLaunchColumnIndex(secondColumnIndex) ||
                !_session.CanLaunchFromColumn(firstColumnIndex) ||
                !_session.CanLaunchFromColumn(secondColumnIndex))
            {
                return false;
            }

            var firstPot = _session.LaunchColumns[firstColumnIndex].PeekTop();
            var secondPot = _session.LaunchColumns[secondColumnIndex].PeekTop();
            return firstPot != null &&
                secondPot != null &&
                firstPot.IsLinked &&
                secondPot.IsLinked &&
                firstPot.LinkedPairId == secondPot.LinkedPairId;
        }

        private bool IsValidLaunchColumnIndex(int columnIndex)
        {
            return _session != null &&
                columnIndex >= 0 &&
                columnIndex < _session.LaunchColumns.Count &&
                _session.LaunchColumns[columnIndex].CanLaunch;
        }

        private void CompleteTutorial()
        {
            _progressService?.MarkCompleted();
            _step = TutorialStep.Completed;
            HideVisual();
        }

        private void Hide()
        {
            _step = TutorialStep.Inactive;
            HideVisual();
        }

        private void HideVisual()
        {
            _view?.Hide();
        }

        private enum TutorialStep
        {
            Inactive,
            AwaitingFirstLaunchTap,
            WaitingForQueueLaunchTarget,
            AwaitingQueueLaunchTap,
            WaitingForPotToPark,
            AwaitingWaitingTap,
            AwaitingFinalLaunchTaps,
            Completed
        }
    }
}
