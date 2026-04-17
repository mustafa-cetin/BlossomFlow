using System.Linq;
using nickeltin.DebugPanel.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Editor
{
    public static class DebugPanelEditorValidation
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            Runtime.DebugPanel.ValidatePanelSettings += ValidatePanelSettings;
        }

        private static void ValidatePanelSettings(Runtime.DebugPanel panel)
        {
            var UIDocument = panel.UIDocument;
            if (UIDocument.panelSettings == null)
            {
                var guid = AssetDatabase.FindAssets("t:" + typeof(PanelSettings).FullName).FirstOrDefault();
                if (!string.IsNullOrEmpty(guid))
                {
                    UIDocument.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetDatabase.GUIDToAssetPath(guid));
                }
                else
                {
                    var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                    panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                    panelSettings.referenceResolution = new Vector2Int(1920, 1080);
                    panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                    panelSettings.match = 0.5f;
                    panelSettings.sortingOrder = 100_000;
                    panelSettings.name = "DebugPanelSettings";
                    UIDocument.panelSettings = panelSettings;
                    var path = AssetDatabase.GenerateUniqueAssetPath("Assets/" + panelSettings.name + ".asset");
                    AssetDatabase.CreateAsset(panelSettings, path);
                    AssetDatabase.SaveAssets();
                    
                    Selection.SetActiveObjectWithContext(panelSettings, panel);
                    Selection.SetActiveObjectWithContext(panel, panel);
                    EditorGUIUtility.PingObject(panelSettings);
                    
                    Debug.Log($"No {nameof(panelSettings)} asset found in project, default created at {path}", panel);
                }
            }
        }
    }
}