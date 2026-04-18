using System;
using System.Collections;
using BlossomFlow.Application.Events;
using Reflex.Attributes;
using TMPro;
using UnityEngine;

namespace BlossomFlow.Gameplay.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ConveyorPotCountView : MonoBehaviour
    {
        private const string DefaultCountFormat = "{0}/{1}";

        [SerializeField] private TMP_Text countText;
        [SerializeField] private string countFormat = DefaultCountFormat;
        [SerializeField] private int fallbackMaxPots = 5;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color blockedColor = Color.red;
        [SerializeField] private float blockedColorSeconds = 0.35f;

        private IEventSubscriber _eventSubscriber;
        private IDisposable _countChangedSubscription;
        private IDisposable _entryBlockedSubscription;
        private Coroutine _blockedColorRoutine;
        private int _currentPots;
        private int _maxPots;

        [Inject]
        private void Construct(IEventSubscriber eventSubscriber)
        {
            _eventSubscriber = eventSubscriber;

            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void Awake()
        {
            EnsureText();
            _maxPots = Mathf.Max(1, fallbackMaxPots);
            ApplyNormalColor();
            RefreshText();
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshText();
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopBlockedColorRoutine();
            ApplyNormalColor();
        }

        private void OnValidate()
        {
            fallbackMaxPots = Mathf.Max(1, fallbackMaxPots);
            blockedColorSeconds = Mathf.Max(0f, blockedColorSeconds);
            if (string.IsNullOrEmpty(countFormat))
            {
                countFormat = DefaultCountFormat;
            }
        }

        private void Reset()
        {
            countText = GetComponent<TMP_Text>();
        }

        private void Subscribe()
        {
            if (_eventSubscriber == null)
            {
                return;
            }

            _countChangedSubscription ??= _eventSubscriber.Subscribe<ConveyorPotCountChanged>(OnCountChanged);
            _entryBlockedSubscription ??= _eventSubscriber.Subscribe<ConveyorPotEntryBlocked>(OnEntryBlocked);
        }

        private void Unsubscribe()
        {
            _countChangedSubscription?.Dispose();
            _entryBlockedSubscription?.Dispose();
            _countChangedSubscription = null;
            _entryBlockedSubscription = null;
        }

        private void OnCountChanged(ConveyorPotCountChanged countChanged)
        {
            _currentPots = Mathf.Max(0, countChanged.CurrentPots);
            _maxPots = Mathf.Max(1, countChanged.MaxPots);
            RefreshText();
        }

        private void OnEntryBlocked(ConveyorPotEntryBlocked entryBlocked)
        {
            _currentPots = Mathf.Max(0, entryBlocked.CurrentPots);
            _maxPots = Mathf.Max(1, entryBlocked.MaxPots);
            RefreshText();
            PlayBlockedColorFeedback();
        }

        private void RefreshText()
        {
            EnsureText();
            if (countText == null)
            {
                return;
            }

            countText.text = string.Format(GetCountFormat(), _currentPots, _maxPots);
        }

        private void PlayBlockedColorFeedback()
        {
            EnsureText();
            if (countText == null)
            {
                return;
            }

            StopBlockedColorRoutine();
            _blockedColorRoutine = StartCoroutine(PlayBlockedColorRoutine());
        }

        private IEnumerator PlayBlockedColorRoutine()
        {
            countText.color = blockedColor;

            if (blockedColorSeconds > 0f)
            {
                yield return new WaitForSeconds(blockedColorSeconds);
            }

            ApplyNormalColor();
            _blockedColorRoutine = null;
        }

        private void StopBlockedColorRoutine()
        {
            if (_blockedColorRoutine == null)
            {
                return;
            }

            StopCoroutine(_blockedColorRoutine);
            _blockedColorRoutine = null;
        }

        private void ApplyNormalColor()
        {
            EnsureText();
            if (countText != null)
            {
                countText.color = normalColor;
            }
        }

        private void EnsureText()
        {
            if (countText == null)
            {
                countText = GetComponent<TMP_Text>();
            }
        }

        private string GetCountFormat()
        {
            return string.IsNullOrEmpty(countFormat) ? DefaultCountFormat : countFormat;
        }
    }
}
