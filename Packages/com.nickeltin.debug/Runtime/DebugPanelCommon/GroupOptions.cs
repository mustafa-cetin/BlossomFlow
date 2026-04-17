using System;

namespace nickeltin.DebugPanel.Runtime
{
    [Flags]
    public enum GroupOptions
    {
        None = 0,
        /// <summary>
        /// Horizonal childs alignment
        /// </summary>
        Horizontal = 1, 
        /// <summary>
        /// Vertical childs alignment
        /// </summary>
        Vertical = 1 << 1,
        /// <summary>
        /// Group will have Toggle arrow, allowing for expanding and collapsing group content
        /// Better give this kind of group a name
        /// </summary>
        Expandable = 1 << 2,
        /// <summary>
        /// Froup padding and margin will be disabled, as well as border
        /// </summary>
        Borderless = 1 << 3
    }
}