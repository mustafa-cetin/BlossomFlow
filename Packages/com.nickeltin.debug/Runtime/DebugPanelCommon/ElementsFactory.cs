using System;
using System.Collections.Generic;
using nickeltin.InternalBridge.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    /// <summary>
    /// Base factory for debug panel elements without styling
    /// </summary>
    public class ElementsFactory
    {
        public virtual Button CreateButton(string text, Action pressCallback)
        {
            var button = new Button(pressCallback)
            {
                text = text,
                name = $"Button({text})"
            };
            return button;
        }

        public virtual RepeatButtonElement CreateRepeatButton(string text, Action pressCallback, float delay,
            float interval)
        {
            var button = new RepeatButtonElement(pressCallback, ToMs(delay), ToMs(interval))
            {
                text = text,
                name = $"RepeatButton({text})"
            };
            return button;
            
            long ToMs(float seconds) => (long)(seconds * 1000f);
        }
        
        public virtual Toggle CreateToggle(string text, bool defaultValue, Action<bool> onValueChanged)
        {
            var toggle = new Toggle(text)
            {
                name  = $"Toggle({text})"
            };
            toggle.value = defaultValue;
            toggle.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
            return toggle;
        }
        
        public virtual Label CreateLabel(string text)
        {
            return new Label(text)
            {
                name = $"Label({text})"
            };
        }
        
        public virtual DropdownField CreateDropdown(string label, Func<List<string>> choicesGetter, 
            Action<int> onValueChanged, int defaultIndex = 0)
        {
            var dropdown = new DropdownField(label)
            {
                name = $"Dropdown({label})"
            };
            
            UpdateValues();
            
            // Overriding create menu callback to make its width for the whole field, not just the dropdown button
            dropdown.SetCreateMenuCallback(() => _IGenericMenu.Create(CreateGenericMenuDropdownCallback(), dropdown));
            dropdown.index = defaultIndex;
            dropdown.RegisterCallback<PointerDownEvent>(evt => UpdateValues());
            dropdown.RegisterValueChangedCallback(evt =>
                onValueChanged?.Invoke(dropdown.index));
            
            return dropdown;

            void UpdateValues() => dropdown.choices = choicesGetter();
        }

        protected virtual GenericDropdownMenu CreateGenericMenuDropdownCallback()
        {
            return new GenericDropdownMenu();
        }
    }
}