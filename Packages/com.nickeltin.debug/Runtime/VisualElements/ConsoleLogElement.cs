using UnityEngine;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    public class ConsoleLogElement : Button
    {
        public DebugPanelLogger.Log Log { get; private set; }
        
        public void SetLog(DebugPanelLogger.Log log)
        {
            Log = log;
            text = log.FullMessage;
        }

        public ConsoleLogElement() => clicked += OnClicked;

        ~ConsoleLogElement() => clicked -= OnClicked;

        private void OnClicked()
        {
            GUIUtility.systemCopyBuffer = Log.FullRawMessage;
            // Debug.Log("Clicked!");
        }
    }
}