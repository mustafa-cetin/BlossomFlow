using System;

namespace nickeltin.DebugPanel.Runtime
{
    /// <summary>
    /// Helper to construct tabs in <see cref="DebugPanel"/>
    /// Wraps <see cref="DebugPanel.BeginTabGroup()"/> and <see cref="DebugPanel.EndTabGroup()"/> calls.
    /// </summary>
    public readonly struct TabGroup : IDisposable
    {
        private readonly DebugPanel _panel;

        public TabGroup(DebugPanel panel, string tabName)
        {
            _panel = panel;
            panel.BeginTabGroup(tabName);
        }

        public void Dispose() => _panel.EndTabGroup();
    }
}