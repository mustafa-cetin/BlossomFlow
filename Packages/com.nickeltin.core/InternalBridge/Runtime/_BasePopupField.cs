using System;
using UnityEngine.UIElements;

namespace nickeltin.InternalBridge.Runtime
{
    public static class _BasePopupField
    {
#if UNITY_6000_3_OR_NEWER
        public static void SetCreateMenuCallback<TValueType, TValueChoice>(
            this BasePopupField<TValueType, TValueChoice> instance, Func<AbstractGenericMenu> callback)
        {
            instance.createMenuCallback = callback;
        }
#else
        public static void SetCreateMenuCallback<TValueType, TValueChoice>(
            this BasePopupField<TValueType, TValueChoice> instance, Func<_IGenericMenu> callback)
        {
            instance.createMenuCallback = () => callback?.Invoke();
        }
#endif
    }
}