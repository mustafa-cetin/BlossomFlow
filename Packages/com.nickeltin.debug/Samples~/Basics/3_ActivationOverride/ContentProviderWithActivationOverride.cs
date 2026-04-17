using nickeltin.DebugPanel.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace nickeltin.DebugPanel.Examples.Runtime
{
    /// <summary>
    /// Based on <see cref="ContentProviderExample"/> from first sample but now also overrides <see cref="IInputModule"/>
    /// </summary>
    public class ContentProviderWithActivationOverride : ContentProviderExample, IInputModule
    {
        [SerializeField] private Button _button;
        
        private DebugPanel.Runtime.DebugPanel _debugPanel;
        private bool _buttonPressed;

        private void Awake()
        {
            // Checking is application is playing to subscribe for button event since content provider has [ExecuteAlways] attribute
            if (Application.isPlaying)
            {
                if (_button != null) _button.onClick.AddListener(OnButtonClick);
            }
        }

        private void OnButtonClick() => _buttonPressed = true;

        public override IInputModule GetInputModule(DebugPanel.Runtime.DebugPanel panel)
        {
            _debugPanel = panel;
            return this;
        }

        public bool UpdateInputModule()
        {
            if (_buttonPressed)
            {
                _buttonPressed = false;
                return true;
            }

            // Fallback for default input
            return _debugPanel.DefaultInputModule.UpdateInputModule();
        }
    }
}