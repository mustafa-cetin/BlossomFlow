using nickeltin.DebugPanel.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Editor
{
    internal static class ContextMenus
    {
        private const string CREATE_MENU = "GameObject/UI/Debug Panel" ;
        
        [MenuItem(CREATE_MENU)]
        private static void CreateDebugPanel(MenuCommand command)
        {
            var instance = ObjectFactory.CreateGameObject("DebugPanel", 
                typeof(UIDocument), typeof(DebugPanelLogger), typeof(BuiltInContentProvider), typeof(Runtime.DebugPanel))
                .GetComponent<Runtime.DebugPanel>();
            
            instance.UseStaticBinding = false;

            var contentProvider = instance.GetComponent<BuiltInContentProvider>();
            ComponentUtility.MoveComponentDown(contentProvider);
            
            var parent = command.context as GameObject;
            if (parent != null)
            {
                ObjectFactory.PlaceGameObject(instance.gameObject, parent);
            }
        }
    }
}