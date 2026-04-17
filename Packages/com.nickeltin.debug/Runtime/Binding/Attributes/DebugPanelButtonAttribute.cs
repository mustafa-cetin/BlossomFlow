using System;

namespace nickeltin.DebugPanel.Runtime.Binding
{
    /// <summary>
    /// Method marked with this attribute will appear in a debug panel if it exists, at a specified path.
    /// Method signature:
    ///     - static
    ///     - void
    ///     - no parameters
    /// Works both in the editor and in build.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class DebugPanelButtonAttribute : Attribute
    {
        internal string DebugPanelName;
        internal string Tab;
        internal string[] Path;
        
        /// <summary>
        /// If a group is missing from debug panel hierarchy, button might not be spawned.
        /// If this is true and the same property is enabled in a debug panel, a missing group will be spawned.
        /// </summary>
        public bool AllowToCreateGroups { get; set; } = true;

        /// <summary>
        /// If true, created groups will be created as horizontal.
        /// This property is checked for the first button in a group, so all other buttons preferences will be ignored.
        /// </summary>
        public bool PreferHorizontalGroups { get; set; } = false;

        public DebugPanelButtonAttribute() => Init("", "", "");

        /// <summary>
        /// Creates a button in specified DebugPanel => Tab => Groups[] => with Name
        /// </summary>
        /// <param name="debugPanelName">There might be multiple debug panels, so they are identified by names. If empty, will be ignored and will be added to all debug panels.</param>
        /// <param name="tabName">There might be multiple tabs in a debug panel. If empty, first tab of a panel will be used.</param>
        /// <param name="groups">Path to button might consist of multiple nested groups</param>
        /// <param name="buttonName">Text on the button</param>
        public DebugPanelButtonAttribute(string debugPanelName, string tabName, string[] groups, string buttonName)
        {
            Init(debugPanelName, tabName, string.Join('/', groups, buttonName));
        }
        
        /// <inheritdoc cref="DebugPanelButtonAttribute(string, string, string[], string)"/>
        public DebugPanelButtonAttribute(string tabName, string[] groups, string buttonName) 
            : this("", tabName, groups, buttonName) { }

        /// <inheritdoc cref="DebugPanelButtonAttribute(string, string, string[], string)"/>
        public DebugPanelButtonAttribute(string[] groups, string buttonName)
            : this("", "", groups, buttonName) { }
        
        /// <inheritdoc cref="DebugPanelButtonAttribute(string, string, string)"/>
        public DebugPanelButtonAttribute(string tab, string path) : this("", tab, path) { }
        
        
        /// <inheritdoc cref="DebugPanelButtonAttribute(string, string)"/>
        public DebugPanelButtonAttribute(string path) : this("", "", path) { }

        /// <summary>
        /// Creates a button in specified DebugPanel => Tab => Groups[] => with Name
        /// </summary>
        /// <param name="debugPanelName">There might be multiple debug panels, so they are identified by names. If empty, will be ignored and will be added to all debug panels.</param>
        /// <param name="tab">There might be multiple tabs in debug panel. If empty, first tab of panel will be used.</param>
        /// <param name="path">
        /// Patch consists of groups separated with '/', where last element is button name.
        ///     1. "Player/Health/-10"
        ///     2. "Player/Health/+10"
        ///     This will spawn two buttons (-10 and +10) in the same group.
        /// </param>
        public DebugPanelButtonAttribute(string debugPanelName, string tab, string path) => Init(debugPanelName, tab, path);

        private void Init(string debugPanelName, string tab, string path)
        {
            this.DebugPanelName = debugPanelName;
            this.Tab = tab;
            this.Path = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}