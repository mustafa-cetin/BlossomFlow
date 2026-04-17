using System;
using System.Collections.Generic;
using System.Linq;
using nickeltin.DebugPanel.Runtime;
using nickeltin.InternalBridge.Editor;
using UnityEditor;
using UnityEngine;

namespace nickeltin.DebugPanel.Editor
{
    [CustomEditor(typeof(BuiltInContentProvider))]
    internal class BuiltInContentProviderEditor : UnityEditor.Editor
    {
        public static class Defaults
        {
            public static readonly List<Type> ProviderTypes;

            static Defaults()
            {
                ProviderTypes = TypeCache.GetTypesDerivedFrom<ContentProvider>()
                    .Where(type => !type.IsAbstract)
                    .ToList();
            }
        }
        
        private SerializedProperty _verticalLayout;
        private SerializedProperty _horizontalLayout;
        
        private void OnEnable()
        {
            _verticalLayout = serializedObject.FindProperty(nameof(BuiltInContentProvider._verticalLayout));
            _horizontalLayout = serializedObject.FindProperty(nameof(BuiltInContentProvider._horizontalLayout));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.HelpBox($"This is a default content provider designed mainly for quick showcase. " +
                                    $"Its recommended to create custom content provider inherited from {nameof(ContentProvider)}", MessageType.Info, true);

            if (GUILayout.Button("Replace with custom content provider"))
            {
                var menu =  new GenericMenu();
                if (Defaults.ProviderTypes.Any())
                {
                    foreach (var providerType in Defaults.ProviderTypes)
                    {
                        if (providerType == typeof(BuiltInContentProvider))
                            continue;
                        
                        menu.AddItem(new GUIContent($"{providerType.Name} ({providerType.FullName})"), false, () =>
                        {
                            var go = ((Component)target).gameObject;
                            Undo.DestroyObjectImmediate(target);
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
            
            EditorGUILayout.PropertyField(_verticalLayout);
            EditorGUILayout.PropertyField(_horizontalLayout);
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}