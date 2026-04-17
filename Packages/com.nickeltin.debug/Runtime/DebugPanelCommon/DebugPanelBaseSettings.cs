using System;
using nickeltin.Core.Runtime;
using UnityEngine;

namespace nickeltin.DebugPanel.Runtime
{
    [Serializable]
    public struct DebugPanelBaseSettings
    {
        [Tooltip("Optional Show/Hide button from keyboard")]
        public Optional<KeyCode> HideButton;
        [Tooltip("Optional Show/Hide input for mobile devices (multitouch is used)")]
        public Optional<int> ActivateWithMultitouch;
        [Tooltip("First selected tab of panel")]
        public Optional<string> SelectTabWithNameAtStart;
        [Tooltip("Main tab can be renamed, default name is COMMANDS")]
        public string MainTabName;
        [Tooltip("Console tab will display all logs, can be disabled")]
        public bool UseConsoleTab;
        [Tooltip("Should panel be hidden at start automatically?")]
        public bool HideAtStart;

        public static DebugPanelBaseSettings Default =>
            new()
            {
                HideButton = new Optional<KeyCode>(KeyCode.Escape),
                ActivateWithMultitouch = new Optional<int>(3),
                MainTabName = "COMMANDS",
                UseConsoleTab = true,
                SelectTabWithNameAtStart = new Optional<string>("CONSOLE")
                {
                    Enabled = false
                },
                HideAtStart = true
            };
    }
}