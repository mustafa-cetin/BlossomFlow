using System;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace nickeltin.InternalBridge.Editor
{
    public readonly struct _Toolbar
    {
        public static readonly Type ToolbarType = typeof(Toolbar);

        private static readonly FieldInfo _root_get;

        static _Toolbar()
        {
            _root_get = ToolbarType.GetField("m_Root", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        
        internal readonly Toolbar instance;
        
        internal _Toolbar(Toolbar instance)
        {
            this.instance = instance;
        }

        public VisualElement GetRoot()
        {
            return (VisualElement)_root_get.GetValue(instance);
        }
        
        public static _Toolbar GetInstance()
        {
#if UNITY_6000_3_OR_NEWER
            return new _Toolbar(Toolbar.instance);
#else
            return new _Toolbar(Toolbar.get);
#endif
        }
    }
}