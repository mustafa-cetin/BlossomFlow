using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace nickeltin.DebugPanel.Runtime.Binding
{
    internal static class DebugPanelStaticBindingUtil
    {
        public readonly struct StaticBoundButton
        {
            public readonly DebugPanelButtonAttribute Attribute;
            public readonly MethodInfo Info;

            public void Invoke()
            {
                Info.Invoke(null, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, 
                    null, null, CultureInfo.InvariantCulture);
            }

            public StaticBoundButton(DebugPanelButtonAttribute attribute, MethodInfo info)
            {
                this.Attribute = attribute;
                this.Info = info;
            }
        }
        private class PanelToAttributes : Dictionary<string, List<StaticBoundButton>> { }
        
        private static PanelToAttributes _panelToAttributes;


        public static bool IsBindingsReady
        {
            get
            {
#if UNITY_EDITOR
                return !EditorApplication.isCompiling && !BuildPipeline.isBuildingPlayer;
#else
                return true;
#endif
            }
        }
        
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void Init_Editor() => Cache(FindAllMethods_Editor());

        private static IEnumerable<StaticBoundButton> FindAllMethods_Editor()
        {
            var methods = TypeCache.GetMethodsWithAttribute<DebugPanelButtonAttribute>();
            foreach (var info in methods)
            {
                if (info.IsStatic && info.GetParameters().Length == 0 && info.ReturnType == typeof(void))
                {
                    var attr = info.GetCustomAttribute<DebugPanelButtonAttribute>();
                    yield return new StaticBoundButton(attr, info);
                }
                else
                {
                    Debug.LogError($"{nameof(DebugPanelButtonAttribute)} can only be used on static methods with void return type and no params");
                }
            }
        }
 
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Init_Build() => Cache(FindAllMethods_Build());
        
        private static IEnumerable<StaticBoundButton> FindAllMethods_Build()
        {
            var bindings = DebugPanelProjectSettings.Get();
            foreach (var method in bindings._staticBindedMethods)
            {
                var type = System.Type.GetType(method.Type + "," + method.Assembly)!;
                var info = type.GetMethod(method.Method, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
                var attr = info.GetCustomAttribute<DebugPanelButtonAttribute>();
                yield return new StaticBoundButton(attr, info);
            }
        }
#endif
        
        public static IEnumerable<SerializableMethodInfo> GetSerializableMethods()
        {
            foreach (var method in _panelToAttributes.Values.SelectMany(m => m))
            {
                var type = method.Info.DeclaringType!;
                var s = new SerializableMethodInfo()
                {
                    Assembly = type.Assembly.FullName,
                    Type = type.FullName,
                    Method = method.Info.Name
                };
                yield return s;
            }
        }
        
        private static void Cache(IEnumerable<StaticBoundButton> methods)
        {
            _panelToAttributes = new PanelToAttributes();
            foreach (var method in methods)
            {
                var panel = method.Attribute.DebugPanelName;
                if (_panelToAttributes.ContainsKey(panel)) _panelToAttributes[panel].Add(method);
                else _panelToAttributes.Add(panel, new List<StaticBoundButton>() { method });
            }
        }
        
        public static IEnumerable<StaticBoundButton> GetBindingsForPanel(string panelName)
        {
            if (_panelToAttributes.TryGetValue(panelName, out var panel)) return panel;

            if (_panelToAttributes.TryGetValue("", out var forPanel)) return forPanel;

            return Enumerable.Empty<StaticBoundButton>();
        }
    }
}