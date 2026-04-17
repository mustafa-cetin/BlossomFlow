using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Toggle = UnityEngine.UIElements.Toggle;

namespace nickeltin.DebugPanel.Runtime
{
    public sealed partial class DebugPanel
    {
        public GroupElement BeginTabGroup(string tabName)
        {
            if (_groupsStack.Count > 0)
            {
                Debug.LogError(
                    $"{nameof(BeginTabGroup)} can only be called inside of root group (When all child groups are closed)");
                return null;
            }

            _currentGroup = ElementsFactory.CreateGroupElement(GroupOptions.Vertical, true);
            _contentRoot.Add(_currentGroup);

            AddTab_Internal(tabName, _currentGroup);
            return _currentGroup;
        }

        public GroupElement EndTabGroup()
        {
            var lastGroup = _currentGroup;
            _currentGroup = _mainTabGroup;
            return lastGroup;
        }

        private TabElement AddTab_Internal(string tabName, GroupElement content)
        {
            var tab = new TabElement(tabName, content);
            _tabsRoot.Add(tab);
            return tab;
        }

        public GroupElement BeginGroup(GroupOptions options, string groupName, bool expanded = true)
        {
            return BeginGroup_Internal(ElementsFactory.CreateGroupElement(options, groupName, false, expanded));
        }

        public GroupElement BeginGroup(GroupOptions options, bool expanded = true)
        {
            return BeginGroup_Internal(ElementsFactory.CreateGroupElement(options, false, expanded));
        }

        private GroupElement BeginGroup_Internal(GroupElement group)
        {
            GetCurrentGroup().AddContent(group);
            _groupsStack.Push(group);
            return group;
        }

        public GroupElement EndGroup()
        {
            var group = GetCurrentGroup();
            if (group == _mainTabGroup)
            {
                Debug.LogError($"{nameof(EndGroup)} called more times than {nameof(BeginGroup)}");
                return null;
            }

            return _groupsStack.Pop();
        }

        public Button AddButton(string text, Action pressCallback)
        {
            return ElementsFactory.CreateButton(text, pressCallback);
        }

        public RepeatButtonElement AddRepeatButton(string text, Action pressCallback, float delay = 0.5f,
            float interval = 0.1f)
        {
            return ElementsFactory.CreateRepeatButton(text, pressCallback, delay, interval);
        }

        public Toggle AddToggle(string text, bool defaultValue, Action<bool> onValueChanged)
        {
            return ElementsFactory.CreateToggle(text, defaultValue, onValueChanged);
        }

        /// <summary>
        /// Experimental
        /// </summary>
        internal Toggle AddToggle(string text, DynamicValue<bool> dynamicValue)
        {
            var instance = AddToggle(text, dynamicValue.Value, b => dynamicValue.Value = b);
            dynamicValue.Changed += () => { instance.SetValueWithoutNotify(dynamicValue.Value); };
            return instance;
        }

        public Label AddLabel(string text)
        {
            return ElementsFactory.CreateLabel(text);
        }

        public DropdownField AddDropdown(string label, Func<List<string>> choicesGetter, Action<int> onValueChanged,
            int defaultIndex = 0)
        {
            return ElementsFactory.CreateDropdown(label, choicesGetter, onValueChanged, defaultIndex);
        }

        public void AddElementToCurrentGroup(VisualElement element)
        {
            GetCurrentGroup().AddContent(element);
        }
    }
}