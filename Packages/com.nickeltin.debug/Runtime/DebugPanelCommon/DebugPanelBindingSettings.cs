using System;
using UnityEngine;

namespace nickeltin.DebugPanel.Runtime
{
    [Serializable]
    public struct DebugPanelBindingSettings
    {
        [Tooltip("Name used to match this panel. Static binding can target different panels")]
        public string BindingName;
        [Tooltip("If true static binded element will create missing hierarchy groups. For complex hierarchy its better to define them in ContentProviderInstead")]
        public bool AllowToCreateNewGroups;

        public static DebugPanelBindingSettings Default =>
            new()
            {
                BindingName = "DebugPanel",
                AllowToCreateNewGroups = true
            };
    }
}