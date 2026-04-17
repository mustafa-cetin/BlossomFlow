using System.Linq;
using nickeltin.Core.Editor;
using nickeltin.DebugPanel.Runtime;
using nickeltin.InternalBridge.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace nickeltin.DebugPanel.Editor
{
    [CustomEditor(typeof(Runtime.DebugPanel)), CanEditMultipleObjects]
    internal class DebugPanelEditor : UnityEditor.Editor
    {
        private static class Defaults
        {
            public static GUIStyle HelpBox =  new(EditorStyles.helpBox)
            {
                richText = true,
            };
        }
        
        private SerializedProperty _baseSettings;
        private SerializedProperty _useStaticBinding;
        private SerializedProperty _bindingSettings;
        private SerializedProperty _stripFromReleaseBuild;
        private SerializedProperty _currentStyleSheet;
        private SerializedProperty _additionalStyleSheets;
        private SerializedProperty _treeAsset;
        private SerializedProperty _panelSettings;
        // private SerializedProperty _customStyleSheet;
        
        // private DebugPanelThemes _themesAsset;
        // private StyleSheet[] _themeStyleSheets;
        // private string[] _themeNames;
        // private int _selectedThemeIndex = -1;
        
        private void OnEnable()
        {
            _baseSettings = serializedObject.FindProperty(nameof(Runtime.DebugPanel._baseSettings));
            _useStaticBinding = serializedObject.FindProperty(nameof(Runtime.DebugPanel._useStaticBinding));
            _bindingSettings = serializedObject.FindProperty(nameof(Runtime.DebugPanel._bindingSettings));
            _stripFromReleaseBuild = serializedObject.FindProperty(nameof(Runtime.DebugPanel._stripFromReleaseBuild));
            _currentStyleSheet = serializedObject.FindProperty(nameof(Runtime.DebugPanel._currentStyleSheet));
            _treeAsset = serializedObject.FindProperty(nameof(Runtime.DebugPanel._treeAsset));
            _panelSettings = serializedObject.FindProperty(nameof(Runtime.DebugPanel._panelSettings));
            _additionalStyleSheets = serializedObject.FindProperty(nameof(Runtime.DebugPanel._additionalStyleSheets));
            // _customStyleSheet = serializedObject.FindProperty(nameof(Runtime.DebugPanel._customStyleSheet));
            
            // var guids = AssetDatabase.FindAssets($"t:{typeof(DebugPanelThemes).FullName}");
            // if (guids.Any())
            // {
            //     _themesAsset = AssetDatabase.LoadAssetAtPath<DebugPanelThemes>(AssetDatabase.GUIDToAssetPath(guids.FirstOrDefault()));
            // }
        }
        
        // private void LoadThemeStyleSheets()
        // {
        //     var styleSheetGuids = AssetDatabase.FindAssets($"t:{nameof(StyleSheet)}");
        //     var themeList = new System.Collections.Generic.List<StyleSheet>();
        //     var nameList = new System.Collections.Generic.List<string>();
        //     
        //     foreach (var guid in styleSheetGuids)
        //     {
        //         var path = AssetDatabase.GUIDToAssetPath(guid);
        //         var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        //         
        //         // Filter for DebugPanelStyle*.uss files
        //         if (fileName.StartsWith("DebugPanelStyle") && path.EndsWith(".uss"))
        //         {
        //             var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
        //             if (styleSheet != null)
        //             {
        //                 themeList.Add(styleSheet);
        //                 nameList.Add(fileName);
        //             }
        //         }
        //     }
        //     
        //     _themeStyleSheets = themeList.ToArray();
        //     _themeNames = nameList.ToArray();
        // }
        
        // private void UpdateSelectedThemeIndex()
        // {
        //     _selectedThemeIndex = -1;
        //     if (_themeStyleSheets == null || _currentStyleSheet.objectReferenceValue == null)
        //         return;
        //         
        //     var currentStyleSheet = _currentStyleSheet.objectReferenceValue as StyleSheet;
        //     if (currentStyleSheet != null)
        //     {
        //         for (int i = 0; i < _themeStyleSheets.Length; i++)
        //         {
        //             if (_themeStyleSheets[i] == currentStyleSheet)
        //             {
        //                 _selectedThemeIndex = i;
        //                 break;
        //             }
        //         }
        //     }
        // }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (!serializedObject.isEditingMultipleObjects)
            {
                var panel = ((Runtime.DebugPanel)target);
                var contentProvider = panel._contentProvider;
                // Using cast to unity object for proper == operator overload
                if (contentProvider == null || contentProvider is BuiltInContentProvider.EmptyImplementation || (contentProvider is Object uObj && uObj == null))
                {
                    _EditorGUILayout.RichTextHelpBox($"No content provider, attach <b>{nameof(BuiltInContentProvider)}</b> or any custom implementation", MessageType.Warning);
                    if (GUILayout.Button($"Add ContentProvider"))
                    {
                        var menu =  new GenericMenu();
                        if (BuiltInContentProviderEditor.Defaults.ProviderTypes.Any())
                        {
                            foreach (var providerType in BuiltInContentProviderEditor.Defaults.ProviderTypes)
                            {
                                menu.AddItem(new GUIContent($"{providerType.Name} ({providerType.FullName})"), false, () =>
                                {
                                    var go = ((Component)target).gameObject;
                                    ObjectFactory.AddComponent(go, providerType);
                                });
                            }
                        }
                        else
                        {
                            menu.AddDisabledItem(new GUIContent("No custom content providers found"));
                        }
                        menu.ShowAsContext();
                    }
                }
                else
                {
                    _EditorGUILayout.RichTextHelpBox($"Current Content Provider: <b>{contentProvider.GetType().Name}</b>", MessageType.Info);
                }
            }
            
            _EditorGUILayout.DrawSectionWithFoldout(_baseSettings, "Base Settings", () =>
            {
                _baseSettings.DrawVisibleChildren();
            });
            
            _EditorGUILayout.DrawSectionWithFoldout(_useStaticBinding, "Static Binding", () =>
            {
                EditorGUILayout.PropertyField(_useStaticBinding);
                using (new EditorGUI.DisabledScope(!_useStaticBinding.boolValue))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        _bindingSettings.DrawVisibleChildren();
                    }
                }
            });
            
            
            _EditorGUILayout.DrawSectionWithFoldout(_panelSettings, "UI Assets", () =>
            {
                EditorGUILayout.PropertyField(_panelSettings);
                EditorGUILayout.PropertyField(_treeAsset);
                EditorGUILayout.PropertyField(_currentStyleSheet);
                EditorGUILayout.PropertyField(_additionalStyleSheets);
                // StyleSheetGUI();
            });
            
            EditorGUILayout.PropertyField(_stripFromReleaseBuild);
            
            serializedObject.ApplyModifiedProperties();
        }

        // private void StyleSheetGUI()
        // {
        //     // Case 1: no theme asset found, just display the plain theme field
        //     if (_themesAsset == null)
        //     {
        //         EditorGUILayout.PropertyField(_currentStyleSheet);
        //         return;
        //     }
        //     
        //     // Case 2: theme asset found, displaying custom toggle 
        //     EditorGUILayout.PropertyField(_customStyleSheet);
        //     
        //     // Case 2.1: displaying a custom style sheet
        //     if (_customStyleSheet.boolValue)
        //     {
        //         EditorGUILayout.PropertyField(_currentStyleSheet);
        //         return;
        //     }
        //
        //     using (new EditorGUILayout.HorizontalScope())
        //     {
        //         EditorGUILayout.PrefixLabel("Current Theme");
        //         var name = "<none>";
        //         var currentStyleSheet = _currentStyleSheet.objectReferenceValue as StyleSheet;
        //         if (currentStyleSheet != null)
        //         {
        //             name = _themesAsset.TryGet(currentStyleSheet, out var theme) ? theme.Name : currentStyleSheet.name;
        //         }
        //         
        //         if (EditorGUILayout.DropdownButton(new GUIContent(name), FocusType.Keyboard))
        //         {
        //             var menu = new GenericMenu();
        //
        //             foreach (var theme in _themesAsset.Themes)
        //             {
        //                 menu.AddItem(new GUIContent(theme.Name), theme.StyleSheet == currentStyleSheet, () =>
        //                 {
        //                     _currentStyleSheet.objectReferenceValue = theme.StyleSheet;
        //                     serializedObject.ApplyModifiedProperties();
        //                 });
        //             }
        //             menu.ShowAsContext();
        //         }
        //     }
        // }
    }
}