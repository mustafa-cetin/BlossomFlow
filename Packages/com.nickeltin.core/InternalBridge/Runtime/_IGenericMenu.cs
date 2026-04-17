using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace nickeltin.InternalBridge.Runtime
{
#if UNITY_6000_3_OR_NEWER
    public class _IGenericMenu : AbstractGenericMenu
    {
        public readonly AbstractGenericMenu instance;

        public readonly VisualElement visualInputOverride;
        
        internal _IGenericMenu(AbstractGenericMenu instance, VisualElement visualInputOverride = null)
        {
            this.instance = instance;
            this.visualInputOverride = visualInputOverride;
        }

        public override void AddItem(string itemName, bool isChecked, Action action)
        {
            instance.AddItem(itemName, isChecked, action);    
        }

        public override void AddItem(string itemName, bool isChecked, Action<object> action, object data)
        {
            instance.AddItem(itemName, isChecked, action, data);
        }

        public override void AddDisabledItem(string itemName, bool isChecked)
        {
            instance.AddDisabledItem(itemName, isChecked);
        }

        public override void AddSeparator(string path)
        {
            instance.AddSeparator(path);
        }

        public override void DropDown(Rect position, VisualElement targetElement, DropdownMenuSizeMode dropdownMenuSizeMode = DropdownMenuSizeMode.Auto)
        {
            if (visualInputOverride != null) position = visualInputOverride.worldBound;

            instance.DropDown(position, targetElement, dropdownMenuSizeMode);
        }
        

        public static _IGenericMenu Create(GenericDropdownMenu instance, VisualElement visualInputOverride = null)
        {
            return new _IGenericMenu(instance, visualInputOverride);
        }
    }

#else
    public readonly struct _IGenericMenu : IGenericMenu
    {
        internal readonly IGenericMenu instance;

        public readonly VisualElement visualInputOverride;
        
        internal _IGenericMenu(IGenericMenu instance, VisualElement visualInputOverride = null)
        {
            this.instance = instance;
            this.visualInputOverride = visualInputOverride;
        }

        public void AddItem(string itemName, bool isChecked, Action action)
        {
            instance.AddItem(itemName, isChecked, action);    
        }

        public void AddItem(string itemName, bool isChecked, Action<object> action, object data)
        {
            instance.AddItem(itemName, isChecked, action, data);
        }

        public void AddDisabledItem(string itemName, bool isChecked)
        {
            instance.AddDisabledItem(itemName, isChecked);
        }

        public void AddSeparator(string path)
        {
            instance.AddSeparator(path);
        }
        

        public void DropDown(Rect position, VisualElement targetElement = null, bool anchored = false)
        {
            if (visualInputOverride != null) position = visualInputOverride.worldBound;

            instance.DropDown(position, targetElement, anchored);
        }

        public static _IGenericMenu Create(GenericDropdownMenu instance, VisualElement visualInputOverride = null)
        {
            return new _IGenericMenu(instance, visualInputOverride);
        }
    }
#endif
}