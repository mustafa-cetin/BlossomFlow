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

        private IFirstLevelTutorialProgressService _progressService;
        private FirstLevelTutorialView _view;
        private LevelPresentationView _presentationView;
        private LevelSession _session;
        private TutorialStep _step;
        private int _guidedLaunchColumn = -1;
        private int _guidedWaitingSlot = -1;
        private int _launchTapCount;
        private readonly List<int> _guidedFinalLaunchColumns = new();

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
                TutorialStep.AwaitingWaitingTap => target.Is(LevelClickTargetKind.WaitingSlot, _guidedWaitingSlot),
                TutorialStep.AwaitingFinalLaunchTaps => IsGuidedFinalLaunchColumn(target),
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
                RemoveGuidedFinalLaunchColumns(target.Index);
                if (_guidedFinalLaunchColumns.Count == 0)
                {
                    CompleteTutorial();
                    return;
                }

                ShowFinalLaunchPrompt();
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
            _guidedLaunchColumn = -1;
            _guidedWaitingSlot = -1;
            _launchTapCount = 0;
            _guidedFinalLaunchColumns.Clear();
            Hide();
        }

        private void OnDestroy()
        {
            UnbindSession();
        }

        private void ShowLaunchPrompt()
        {
            _guidedLaunchColumn = FindFirstLaunchableColumn();
            if (_guidedLaunchColumn < 0 ||
                _presentationView == null ||
                !_presentationView.TryGetLaunchColumnAnchor(_guidedLaunchColumn, out var position))
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
            _view.PointAt(position + launchTargetOffset, prompt);
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

            for (var i = 0; i < _session.WaitingSlots.Slots.Count; i++)
            {
                var slot = _session.WaitingSlots.Slots[i];
                if (slot == null || !slot.IsOccupied)
                {
                    continue;
                }

                ShowWaitingPrompt(slot.Index);
                return true;
            }

            return false;
        }

        private void ShowWaitingPrompt(int slotIndex)
        {
            _guidedWaitingSlot = slotIndex;
            _step = TutorialStep.AwaitingWaitingTap;
            StartCoroutine(PointAtWaitingSlotNextFrame(slotIndex));
        }

        private int FindFirstLaunchableColumn()
        {
            if (_session == null)
            {
                return -1;
            }

            for (var i = 0; i < _session.LaunchColumns.Count; i++)
            {
                if (_session.CanLaunchFromColumn(i))
                {
                    return i;
                }
            }

            return -1;
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

            ShowWaitingPrompt(slotIndex);
        }

        private System.Collections.IEnumerator PointAtWaitingSlotNextFrame(int slotIndex)
        {
            yield return null;

            if (_step != TutorialStep.AwaitingWaitingTap ||
                _presentationView == null ||
                !_presentationView.TryGetWaitingPotAnchor(slotIndex, out var position))
            {
                yield break;
            }

            _view.PointAt(position + waitingTargetOffset, waitingPrompt);
        }

        private void OnWaitingPotRecalled(LevelSession session, PotModel pot, int slotIndex)
        {
            if (_step != TutorialStep.AwaitingWaitingTap || slotIndex != _guidedWaitingSlot)
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
            _guidedFinalLaunchColumns.Clear();
            var positions = new List<Vector3>();

            if (_session == null || _presentationView == null)
            {
                return false;
            }

            for (var i = 0; i < _session.LaunchColumns.Count; i++)
            {
                if (!_session.CanLaunchFromColumn(i) ||
                    !_presentationView.TryGetLaunchColumnAnchor(i, out var position))
                {
                    continue;
                }

                _guidedFinalLaunchColumns.Add(i);
                positions.Add(position + finalLaunchTargetOffset);

                if (_guidedFinalLaunchColumns.Count == 2)
                {
                    break;
                }
            }

            if (_guidedFinalLaunchColumns.Count == 0)
            {
                return false;
            }

            _step = TutorialStep.AwaitingFinalLaunchTaps;
            _view.PointAt(positions, finalLaunchPrompt);
            return true;
        }

        private bool IsGuidedFinalLaunchColumn(LevelClickTarget target)
        {
            return target != null &&
                target.Kind == LevelClickTargetKind.LaunchColumn &&
                ContainsEquivalentGuidedFinalLaunchColumn(target.Index);
        }

        private bool IsGuidedLaunchColumn(LevelClickTarget target)
        {
            return target != null &&
                target.Kind == LevelClickTargetKind.LaunchColumn &&
                AreEquivalentLaunchColumns(target.Index, _guidedLaunchColumn);
        }

        private bool ContainsEquivalentGuidedFinalLaunchColumn(int columnIndex)
        {
            for (var i = 0; i < _guidedFinalLaunchColumns.Count; i++)
            {
                if (AreEquivalentLaunchColumns(columnIndex, _guidedFinalLaunchColumns[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveGuidedFinalLaunchColumns(int clickedColumnIndex)
        {
            for (var i = _guidedFinalLaunchColumns.Count - 1; i >= 0; i--)
            {
                if (AreEquivalentLaunchColumns(clickedColumnIndex, _guidedFinalLaunchColumns[i]))
                {
                    _guidedFinalLaunchColumns.RemoveAt(i);
                }
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
