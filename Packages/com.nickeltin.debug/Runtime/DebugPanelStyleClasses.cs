using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    internal static class DebugPanelStyleClasses
    {
        public const string horizontalGroup = "debug-panel-horizontal";
        public const string verticalGroup = "debug-panel-vertical";
        public const string button = "debug-panel-button";
        public const string repeatButton = "debug-panel-repeat-button";
        public const string toggle = "debug-panel-toggle";
        public const string dropdown = "debug-panel-dropdown";
        public const string group = "debug-panel-group";
        public const string expander = "debug-panel-group-expander";
        public const string tab = "debug-panel-tab";
        public const string scrollView = "debug-panel-scroll-view";
        public const string selectedTab = "debug-panel-selected-tab";
        public const string hidden = "hidden";
        public const string console = "debug-panel-console";
        public const string borderless = "borderless";
        public const string expandable = "expandable";
        
        
        public static void SetHidden(this VisualElement element, bool isHidden)
        {
            if (isHidden)
                element.AddToClassList(hidden);
            else
                element.RemoveFromClassList(hidden);
        }

        public static void ToggleHidden(this VisualElement element)
        {
            element.ToggleInClassList(hidden);
        }
    }
}