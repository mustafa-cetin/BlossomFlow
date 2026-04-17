using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    public sealed class TabElement : Button
    {
        private readonly VisualElement _content;

        public event Action onSelect;
        
        private bool selected => ClassListContains(DebugPanelStyleClasses.selectedTab);
        
        public TabElement(string label, GroupElement content)
        {
            GroupElement.SetTab(content, this);
            _content = content;
            text = label;
            name = "Tab" + label;
            RegisterCallback<ClickEvent>(OnClick);
            RegisterCallback<NavigationSubmitEvent>(OnSubmit);
            AddToClassList(DebugPanelStyleClasses.tab);
        }
        
        private void OnClick(ClickEvent evt)
        {
            if (selected) return;
            Select();
        }
        
        private void OnSubmit(NavigationSubmitEvent evt)
        {
            if (selected) return;
            Select();
            evt.StopPropagation();
        }

        public void Select()
        {
            FindActiveTabAndDisable();
            
            AddToClassList(DebugPanelStyleClasses.selectedTab);
            _content.SetHidden(false);
            _content.SetEnabled(true);
            onSelect?.Invoke();
        }

        private void Deselect()
        {
            RemoveFromClassList(DebugPanelStyleClasses.selectedTab);
            _content.SetHidden(true);
            _content.SetEnabled(false);
        }

        private void FindActiveTabAndDisable()
        {
            var activeTab = parent.Q<TabElement>(null, DebugPanelStyleClasses.selectedTab);
            if (activeTab != null)
            {
                activeTab.Deselect();
            }
        }

        public static IEnumerable<TabElement> GetTabs(VisualElement tabsRoot) => tabsRoot.Query<TabElement>().Build();

        public static TabElement FirstOrDefault(VisualElement tabsRoot, string tabName)
        {
            return tabsRoot.Q<TabElement>("Tab" + tabName);
        }

        /// <summary>
        /// Deselects all tabs, then selects first.
        /// </summary>
        public static void Initialize(VisualElement tabsRoot)
        {
            foreach (var tab in GetTabs(tabsRoot)) tab.Deselect();
            
            tabsRoot.Q<TabElement>()?.Select();
        }

    }
}