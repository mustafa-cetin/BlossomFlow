using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BlossomFlow.Gameplay.Presentation.Tutorial
{
    public sealed class FirstLevelTutorialView : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject handPrefab;
        [SerializeField] private TextMeshProUGUI textPrefab;
        [SerializeField] private Canvas worldCanvas;

        [Header("Layout")]
        [SerializeField] private Vector3 handOffset = new Vector3(0.28f, 0.05f, -1f);
        [SerializeField] private Vector3 labelOffset = new Vector3(0f, 0.56f, -1f);
        [SerializeField] private float pulseDistance = 0.18f;
        [SerializeField] private float pulseSeconds = 0.55f;

        private readonly List<Transform> _hands = new();
        private readonly List<Sequence> _pulseSequences = new();
        private TextMeshProUGUI _label;

        public void PointAt(Vector3 worldPosition, string text)
        {
            PointAt(new[] { worldPosition }, worldPosition, text);
        }

        public void PointAt(IReadOnlyList<Vector3> worldPositions, string text)
        {
            PointAt(worldPositions, null, text);
        }

        public void PointAt(IReadOnlyList<Vector3> worldPositions, Vector3? labelWorldPosition, string text)
        {
            EnsureVisuals(worldPositions?.Count ?? 0);
            if ((_hands.Count == 0 && _label == null) || worldPositions == null || worldPositions.Count == 0)
            {
                return;
            }

            KillPulseSequences();
            HideHands();

            for (var i = 0; i < worldPositions.Count; i++)
            {
                if (i >= _hands.Count || _hands[i] == null)
                {
                    continue;
                }

                _hands[i].gameObject.SetActive(true);
                RestartPulse(i, worldPositions[i]);
            }

            if (_label != null)
            {
                var center = labelWorldPosition ?? Vector3.zero;
                if (!labelWorldPosition.HasValue)
                {
                    for (var i = 0; i < worldPositions.Count; i++)
                    {
                        center += worldPositions[i];
                    }

                    center /= worldPositions.Count;
                }

                _label.gameObject.SetActive(true);
                _label.transform.position = center + labelOffset;
                _label.text = text ?? string.Empty;
            }
        }

        public void SetWorldCanvas(Canvas canvas)
        {
            worldCanvas = canvas;
        }

        public void Hide()
        {
            KillPulseSequences();
            HideHands();

            if (_label != null)
            {
                _label.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            KillPulseSequences();
        }

        private void EnsureVisuals(int handCount)
        {
            while (_hands.Count < handCount && handPrefab != null)
            {
                var parent = handPrefab.GetComponent<RectTransform>() != null && worldCanvas != null
                    ? worldCanvas.transform
                    : transform;

                var hand = Instantiate(handPrefab, parent).transform;
                hand.gameObject.SetActive(false);
                _hands.Add(hand);
            }

            if (_label == null && textPrefab != null)
            {
                var parent = worldCanvas != null ? worldCanvas.transform : transform;
                _label = Instantiate(textPrefab, parent);
                _label.gameObject.SetActive(false);
            }
        }

        private void HideHands()
        {
            for (var i = 0; i < _hands.Count; i++)
            {
                if (_hands[i] != null)
                {
                    _hands[i].gameObject.SetActive(false);
                }
            }
        }

        private void RestartPulse(int handIndex, Vector3 targetPosition)
        {
            if (handIndex < 0 || handIndex >= _hands.Count || _hands[handIndex] == null)
            {
                return;
            }

            EnsurePulseSequenceSlot(handIndex);
            _pulseSequences[handIndex]?.Kill();

            var handPosition = targetPosition + handOffset;
            var hand = _hands[handIndex];
            hand.position = handPosition;

            var sequence = DOTween.Sequence()
                .SetLink(hand.gameObject, LinkBehaviour.KillOnDestroy)
                .SetLoops(-1, LoopType.Restart);

            sequence.Append(hand.DOMove(handPosition + Vector3.up * pulseDistance, pulseSeconds).SetEase(Ease.InOutSine));
            sequence.Append(hand.DOMove(handPosition, pulseSeconds).SetEase(Ease.InOutSine));
            _pulseSequences[handIndex] = sequence;
        }

        private void EnsurePulseSequenceSlot(int index)
        {
            while (_pulseSequences.Count <= index)
            {
                _pulseSequences.Add(null);
            }
        }

        private void KillPulseSequences()
        {
            for (var i = 0; i < _pulseSequences.Count; i++)
            {
                _pulseSequences[i]?.Kill();
                _pulseSequences[i] = null;
            }
        }
    }
}
