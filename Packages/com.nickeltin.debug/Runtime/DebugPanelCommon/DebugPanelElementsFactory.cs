using System;
using System.Collections.Generic;
using nickeltin.InternalBridge.Runtime;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    /// <summary>
    /// Version of factory that adds all debug panel styling <see cref="DebugPanelStyleClasses"/>.
    /// Also upon creation adds elements to the current <see cref="DebugPanel"/> group.
    /// </summary>
    public class DebugPanelElementsFactory : ElementsFactory
    {
        private readonly DebugPanel _debugPanel;

        public DebugPanelElementsFactory(DebugPanel debugPanel)
        {
            _debugPanel = debugPanel;
        }

        public override Button CreateButton(string text, Action pressCallback)
        {
            var instance = base.CreateButton(text, pressCallback);
            return AddElement(instance, DebugPanelStyleClasses.button);
        }

        public override RepeatButtonElement CreateRepeatButton(string text, Action pressCallback, float delay, float interval)
        {
            var instance = base.CreateRepeatButton(text, pressCallback, delay, interval);
            return AddElement(instance, DebugPanelStyleClasses.button, DebugPanelStyleClasses.repeatButton);
        }

        public override Toggle CreateToggle(string text, bool defaultValue, Action<bool> onValueChanged)
        {
            var instance = base.CreateToggle(text, defaultValue, onValueChanged);
            return AddElement(instance, DebugPanelStyleClasses.toggle);
        }


        public override Label CreateLabel(string text)
        {
            return AddElement(base.CreateLabel(text));
        }

        public override DropdownField CreateDropdown(string label, Func<List<string>> choicesGetter, Action<int> onValueChanged, int defaultIndex = 0)
        {
            var instance = base.CreateDropdown(label, choicesGetter, onValueChanged, defaultIndex);
            return AddElement(instance, DebugPanelStyleClasses.dropdown);
        }

        protected override GenericDropdownMenu CreateGenericMenuDropdownCallback()
        {
            var instance = base.CreateGenericMenuDropdownCallback();
            var container = instance.GetMenuContainer();
            // Overriding style sheets to since a dropdown menu spawns not in the root of a panel
            // This makes dropdown inherit panel style
            foreach (var sheet in _debugPanel.GetAllStyleSheets()) container.styleSheets.Add(sheet);
            return instance;
        }

        public T AddElement<T>(T instance, params string[] classes) where T : VisualElement
        {
            foreach (var @class in classes) instance.AddToClassList(@class);
            _debugPanel.AddElementToCurrentGroup(instance);
            return instance;
        }
        
        public ScrollViewElement CreateScrollView()
        {
            var scrollView = new ScrollViewElement();
            scrollView.AddToClassList(DebugPanelStyleClasses.scrollView);
            
            // Configure scroll speed
            scrollView.verticalPageSize = 1000f;
            scrollView.mouseWheelScrollSize = 500f;
            
            return scrollView;
        }

        
        
        /// <summary>
        /// Setups scroll view to work the same as <see cref="CreateScrollView"/>
        /// </summary>
        public static void SetupScrollView(ScrollView scrollView)
        {
            scrollView.AddToClassList(DebugPanelStyleClasses.scrollView);
            
            scrollView.RegisterAutoScrollToCenter();
            scrollView.schedule.Execute(scrollView.ConfigureScrollerFocusability);
            
            scrollView.verticalPageSize = 1000f;
            scrollView.mouseWheelScrollSize = 500f;
        }
        
        public GroupElement CreateGroupElement(GroupOptions options, bool useScrollView = false, bool expanded = true)
        {
            return new GroupElement(options, useScrollView, this)
            {
                Expanded = expanded
            };
        }
        
        public GroupElement CreateGroupElement(GroupOptions options, string label, bool useScrollView = false, bool expanded = true)
        {
            var group = CreateGroupElement(options, useScrollView, expanded);
            group.AddLabel(label);
            return group;
        }
    }
}