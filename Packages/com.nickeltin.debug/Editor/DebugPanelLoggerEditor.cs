using nickeltin.DebugPanel.Runtime;
using UnityEditor;

namespace nickeltin.DebugPanel.Editor
{
    [CustomEditor(typeof(DebugPanelLogger)), CanEditMultipleObjects]
    internal class DebugPanelLoggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _captureLogsFromCategory;
        private SerializedProperty _maxLogsCount;
        private SerializedProperty _logStacktrace;

        private void OnEnable()
        {
            _captureLogsFromCategory = serializedObject.FindProperty(nameof(DebugPanelLogger._captureLogsFromCategory));
            _maxLogsCount = serializedObject.FindProperty(nameof(DebugPanelLogger._maxLogsCount));
            _logStacktrace = serializedObject.FindProperty(nameof(DebugPanelLogger._logStacktrace));
        }

        override public void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_captureLogsFromCategory);
            EditorGUILayout.PropertyField(_maxLogsCount);
            EditorGUILayout.PropertyField(_logStacktrace);

            if (!serializedObject.isEditingMultipleObjects)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("Current logs count", ((DebugPanelLogger)target).CurrentLogsCount);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}