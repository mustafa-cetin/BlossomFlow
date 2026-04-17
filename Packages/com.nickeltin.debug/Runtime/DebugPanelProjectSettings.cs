using System;
using System.Linq;
using nickeltin.DebugPanel.Runtime.Binding;
using nickeltin.ProjectSettings.Runtime;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Build.Reporting;
#endif

namespace nickeltin.DebugPanel.Runtime
{
    [ProjectSettings("DebugPanel", HideInEditor = true)]
    internal class DebugPanelProjectSettings : ProjectSettingsAsset<DebugPanelProjectSettings>
    {
        [SerializeField, HideInInspector] internal SerializableMethodInfo[] _staticBindedMethods = Array.Empty<SerializableMethodInfo>();
        
#if UNITY_EDITOR
        public override void OnBuild(BuildReport buildReport)
        {
            _staticBindedMethods = DebugPanelStaticBindingUtil.GetSerializableMethods().ToArray();
        }
#endif
    }
}