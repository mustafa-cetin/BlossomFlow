using System;
using nickeltin.Core.Runtime;
using UnityEngine;
using TouchPhase = UnityEngine.TouchPhase;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace nickeltin.DebugPanel.Runtime
{
    public sealed class DefaultInputModule : IInputModule
    {
        private readonly DebugPanel _debugPanel;
        private Optional<KeyCode> _hideButton => _debugPanel.BaseSettings.HideButton;
        private Optional<int> _activateWithMultitouch => _debugPanel.BaseSettings.ActivateWithMultitouch;
        
        private bool _multitouchActivated;

        public DefaultInputModule(DebugPanel panel)
        {
            _debugPanel = panel;
        }
        
        public bool UpdateInputModule()
        {
#if ENABLE_INPUT_SYSTEM
            return CaptureNewInput();
#else
            return CaptureLegacyInput();
#endif
        }

        private bool CaptureLegacyInput()
        {
            if (_activateWithMultitouch)
            {
                var touches = Input.touches;
                var activeCount = 0;
                var hasNewTouch = false;
                
                for (var i = 0; i < touches.Length; i++)
                {
                    var phase = touches[i].phase;
                    if (phase == TouchPhase.Began || phase == TouchPhase.Moved || phase == TouchPhase.Stationary)
                    {
                        activeCount++;
                        if (phase == TouchPhase.Began)
                        {
                            hasNewTouch = true;
                        }
                    }
                }
                
                // Trigger when we reach the touch threshold and at least one new touch was detected
                if (activeCount >= _activateWithMultitouch && hasNewTouch && !_multitouchActivated)
                {
                    _multitouchActivated = true;
                    return true;
                }
                
                // Reset flag when touch count drops below a threshold
                if (activeCount < _activateWithMultitouch)
                {
                    _multitouchActivated = false;
                }
            }
            
            if (_hideButton.Enabled && Input.GetKeyDown(_hideButton))
            {
                return true;
            }

            return false;
        }

#if ENABLE_INPUT_SYSTEM
        private bool CaptureNewInput()
        {
            if (_activateWithMultitouch)
            {
                var touchscreen = Touchscreen.current;
                if (touchscreen != null)
                {
                    var touches = touchscreen.touches;
                    var activeCount = 0;
                    var hasNewTouch = false;
                    
                    for (var i = 0; i < touches.Count; i++)
                    {
                        var touchControl = touches[i];
                        if (touchControl.press.isPressed)
                        {
                            activeCount++;
                            if (touchControl.press.wasPressedThisFrame)
                            {
                                hasNewTouch = true;
                            }
                        }
                    }
                    
                    // Trigger when we reach the touch threshold and at least one new touch was detected
                    if (activeCount >= _activateWithMultitouch && hasNewTouch && !_multitouchActivated)
                    {
                        _multitouchActivated = true;
                        return true;
                    }
                    
                    // Reset flag when touch count drops below threshold
                    if (activeCount < _activateWithMultitouch)
                    {
                        _multitouchActivated = false;
                    }
                }
            }

            if (_hideButton.Enabled)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    KeyCode keyCode = _hideButton;
                    if (TryConvertKeyCodeToKey(keyCode, out var key))
                    {
                        var keyControl = keyboard[key];
                        if (keyControl != null && keyControl.wasPressedThisFrame)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool TryConvertKeyCodeToKey(KeyCode keyCode, out Key key)
        {
            switch (keyCode)
            {
                case KeyCode.Escape: key = Key.Escape; return true;
                case KeyCode.Space: key = Key.Space; return true;
                case KeyCode.Return: key = Key.Enter; return true;
                case KeyCode.KeypadEnter: key = Key.NumpadEnter; return true;
                case KeyCode.BackQuote: key = Key.Backquote; return true;
                case KeyCode.Backspace: key = Key.Backspace; return true;
                case KeyCode.Tab: key = Key.Tab; return true;
                default:
                    return Enum.TryParse(keyCode.ToString(), out key);
            }
        }
#endif
    }
}