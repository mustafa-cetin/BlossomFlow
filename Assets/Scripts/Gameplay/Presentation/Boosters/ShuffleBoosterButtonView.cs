using System;
using BlossomFlow.Application.Events;
using BlossomFlow.Gameplay.Domain.Boosters;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlossomFlow.Gameplay.Presentation.Boosters
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ShuffleBoosterButtonView : MonoBehaviour
    {
        private const int DefaultFallbackRemainingUses = 5;

        [SerializeField] private Button button;
        [SerializeField] private TMP_Text remainingUsesText;
        [SerializeField] private string remainingUsesFormat = "{0}";
        [SerializeField] private int fallbackRemainingUses = DefaultFallbackRemainingUses;

        private IEventPublisher _eventPublisher;
        private IEventSubscriber _eventSubscriber;
        private IDisposable _usesChangedSubscription;
        private IDisposable _usedSubscription;
        private IDisposable _unavailableSubscription;
        private int _remainingUses = DefaultFallbackRemainingUses;

        [Inject]
        private void Construct(IEventPublisher eventPublisher, IEventSubscriber eventSubscriber)
        {
            _eventPublisher = eventPublisher;
            _eventSubscriber = eventSubscriber;

            if (isActiveAndEnabled)
            {
                Subscribe();
                Refresh();
            }
        }

        private void Awake()
        {
            EnsureButton();
            _remainingUses = Mathf.Max(0, fallbackRemainingUses);
        }

        private void OnEnable()
        {
            EnsureButton();
            if (button != null)
            {
                button.onClick.AddListener(OnClicked);
            }

            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
            }

            Unsubscribe();
        }

        private void OnValidate()
        {
            fallbackRemainingUses = Mathf.Max(0, fallbackRemainingUses);
            if (string.IsNullOrEmpty(remainingUsesFormat))
            {
                remainingUsesFormat = "{0}";
            }
        }

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        private void OnClicked()
        {
            _eventPublisher?.Publish(new ShuffleBoosterRequested());
        }

        private void Subscribe()
        {
            if (_eventSubscriber == null)
            {
                return;
            }

            _usesChangedSubscription ??= _eventSubscriber.Subscribe<ShuffleBoosterUsesChanged>(OnUsesChanged);
            _usedSubscription ??= _eventSubscriber.Subscribe<ShuffleBoosterUsed>(OnUsed);
            _unavailableSubscription ??= _eventSubscriber.Subscribe<ShuffleBoosterUnavailable>(OnUnavailable);
        }

        private void Unsubscribe()
        {
            _usesChangedSubscription?.Dispose();
            _usedSubscription?.Dispose();
            _unavailableSubscription?.Dispose();
            _usesChangedSubscription = null;
            _usedSubscription = null;
            _unavailableSubscription = null;
        }

        private void OnUsesChanged(ShuffleBoosterUsesChanged usesChanged)
        {
            SetRemainingUses(usesChanged.RemainingUses);
        }

        private void OnUsed(ShuffleBoosterUsed used)
        {
            SetRemainingUses(used.RemainingUses);
        }

        private void OnUnavailable(ShuffleBoosterUnavailable unavailable)
        {
            SetRemainingUses(unavailable.RemainingUses);
        }

        private void SetRemainingUses(int remainingUses)
        {
            _remainingUses = Mathf.Max(0, remainingUses);
            Refresh();
        }

        private void Refresh()
        {
            EnsureButton();
            if (button != null)
            {
                button.interactable = _remainingUses > 0;
            }

            if (remainingUsesText != null)
            {
                remainingUsesText.text = string.Format(GetRemainingUsesFormat(), _remainingUses);
            }
        }

        private void EnsureButton()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        private string GetRemainingUsesFormat()
        {
            return string.IsNullOrEmpty(remainingUsesFormat) ? "{0}" : remainingUsesFormat;
        }
    }
}
