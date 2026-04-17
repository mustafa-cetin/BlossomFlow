#if UNITY_EDITOR

using System;

namespace nickeltin.DebugPanel.Runtime
{
    public sealed partial class DebugPanel
    {
        public static event Action<DebugPanel> ValidatePanelSettings;
        
        private void OnValidate()
        {
            Refresh();
        }

        private void Reset()
        {
            // SetDirty();
            Refresh();
        }
    }
}

#endif