using System;

namespace nickeltin.DebugPanel.Runtime
{
    /// <summary>
    /// Helper to construct groups in <see cref="DebugPanel"/>
    /// Wraps <see cref="DebugPanel.BeginGroup()"/> and <see cref="DebugPanel.EndGroup()"/> calls.
    /// </summary>
    public readonly struct Group : IDisposable
    {
        private readonly DebugPanel _panel;

        public Group(DebugPanel panel, GroupOptions groupOptions, string groupName, bool expanded = true)
        {
            _panel = panel;
            panel.BeginGroup(groupOptions, groupName, expanded);
        }

        public Group(DebugPanel panel, GroupOptions groupOptions, bool expanded = true)
        {
            _panel = panel;
            panel.BeginGroup(groupOptions, expanded);
        }

        public void Dispose() => _panel.EndGroup();
    }
}