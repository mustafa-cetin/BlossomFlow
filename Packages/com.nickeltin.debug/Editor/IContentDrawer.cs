using System;
using System.Collections.Generic;
using System.Linq;
using nickeltin.Core.Editor;
using nickeltin.DebugPanel.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.DebugPanel.Editor
{
    [CustomPropertyDrawer(typeof(BuiltInContentProvider.IContent), true)]
    internal class IContentDrawer : PropertyDrawer
    {
        private static List<Type> _allowedTypes = new();
        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            _allowedTypes = TypeCache.GetTypesDerivedFrom<BuiltInContentProvider.IContent>()
                .Where(type => !type.IsAbstract && type.IsSerializable)
                .Except(new List<Type>()
                {
                    typeof(BuiltInContentProvider.TabGroup)
                })
                .ToList();
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                DrawProperties(property.GetValue());
                return;
            }
            
            var value = property.managedReferenceValue;
            if (value == null)
            {
                var rect = EditorGUI.PrefixLabel(position, label);
                if (EditorGUI.DropdownButton(rect, new GUIContent("(null) - Select Type"), FocusType.Keyboard))
                {
                    var menu = new GenericMenu();
                    foreach (var allowedType in _allowedTypes)
                    {
                        menu.AddItem(new GUIContent(allowedType.Name), false, () =>
                        {
                            var instance = Activator.CreateInstance(allowedType);
                            property.managedReferenceValue = instance;
                            property.serializedObject.ApplyModifiedProperties();
                        });
                    }
                    menu.ShowAsContext();
                }
            }
            else
            {
                DrawProperties(value);
            }
          

            return;
            
            void DrawProperties(object lValue)
            {
                EditorGUI.BeginProperty(position, label, property);
                var rect = position;
                
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(rect, lValue.GetType().Name, EditorStyles.boldLabel);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                foreach (var child in property.GetVisibleChilds())
                {
                    var height = EditorGUI.GetPropertyHeight(child);
                    rect.height = height;
                    EditorGUI.PropertyField(rect, child);
                    rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                }
                
                EditorGUI.EndProperty();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                var value = property.managedReferenceValue;
                if (value == null)
                    return EditorGUIUtility.singleLineHeight;
            }

            var height = 0f;
            foreach (var child in property.GetVisibleChilds())
            {
                height += EditorGUI.GetPropertyHeight(child);
                height += EditorGUIUtility.standardVerticalSpacing;
            }

            return height + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}