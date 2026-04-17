using System;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    /// <summary>
    /// Repeat-capable button that supports both pointer press-and-hold (via base RepeatButton)
    /// and keyboard/remote navigation submit hold (e.g., Amazon Fire TV DPAD_CENTER).
    /// </summary>
    public sealed class RepeatButtonElement : RepeatButton
    {
        private bool _isHeldByNavigation;
        private IVisualElementScheduledItem _repeatScheduler;

        private readonly Action _pressCallback;
        private readonly long _delayMs;
        private readonly long _intervalMs;

        public RepeatButtonElement(Action pressCallback, long delayMs, long intervalMs)
            : base(pressCallback, Math.Max(0, delayMs), Math.Max(0, intervalMs))
        {
            _pressCallback = pressCallback;
            _delayMs = Math.Max(0, delayMs);
            _intervalMs = Math.Max(0, intervalMs);

            focusable = true;
            tabIndex = 0;

            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);

            // Generic end conditions (device-agnostic)
            RegisterCallback<KeyUpEvent>(_ => StopHold());
            RegisterCallback<PointerUpEvent>(_ => StopHold());
            RegisterCallback<FocusOutEvent>(_ => StopHold());
            RegisterCallback<DetachFromPanelEvent>(_ => StopHold());
            RegisterCallback<NavigationMoveEvent>(_ => StopHold());
        }

        private void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            // Invoke immediately on first press (like base RepeatButton)
            _pressCallback?.Invoke();
            
            // Begin hold; start scheduler which will invoke after delay and then at interval
            _isHeldByNavigation = true;
            StartRepeatScheduler();
            evt.StopPropagation();
        }

        private void StartRepeatScheduler()
        {
            // Stop any existing scheduler
            _repeatScheduler?.Pause();
            
            // Start new scheduler: wait for delay, then repeat at interval
            _repeatScheduler = schedule.Execute(() =>
                {
                    if (_isHeldByNavigation)
                    {
                        _pressCallback?.Invoke();
                    }
                })
                .Every(_intervalMs)
                .StartingIn(_delayMs);
        }

        private void StopHold()
        {
            _isHeldByNavigation = false;
            _repeatScheduler?.Pause();
        }
    }
}