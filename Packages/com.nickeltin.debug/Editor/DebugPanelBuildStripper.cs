using System.Collections.Generic;
using System.Linq;
using nickeltin.DebugPanel.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace nickeltin.DebugPanel.Editor
{
    /// <summary>
    /// Processes scenes and removes instances of debug panels upon build if <see cref="DebugPanel._stripFromReleaseBuild"/> is ture.
    /// </summary>
    internal class DebugPanelBuildStripper : IProcessSceneWithReport 
    {
        public int callbackOrder => 0;
        
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            try
            {
                if (report == null || report.summary.options.HasFlag(BuildOptions.Development))
                {
                    return;
                }

                var panels = new List<Runtime.DebugPanel>();

                foreach (var gameObject in scene.GetRootGameObjects())
                {
                    panels.AddRange(gameObject.GetComponentsInChildren<Runtime.DebugPanel>(true));
                }
                
                var strippedPaths = new List<string>();

                foreach (var panel in panels.Where(panel => panel._stripFromReleaseBuild))
                {
                    strippedPaths.Add(GetHierarchyPath(panel));
                    Object.DestroyImmediate(panel.gameObject);
                }

                if (strippedPaths.Count <= 0) return;
                
                var sceneLabel = string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
                Debug.Log($"[DebugPanel] Stripped {strippedPaths.Count} panel(s) from scene '{sceneLabel}':\n- {string.Join(",\n- ", strippedPaths)}");
            }
            catch
            {
                // noop
            }
        }

        private static string GetHierarchyPath(Runtime.DebugPanel panel)
        {
            var t = panel.transform;
            var root = t.root;
            var rel = AnimationUtility.CalculateTransformPath(t, root);
            return string.IsNullOrEmpty(rel) ? root.name : root.name + "/" + rel;
        }
    }
}