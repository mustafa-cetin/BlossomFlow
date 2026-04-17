using UnityEngine.UIElements;

namespace nickeltin.InternalBridge.Runtime
{
    public static class _BaseField
    {
        public static VisualElement GetVisualInput<TValueType>(this BaseField<TValueType> instance)
        {
            return instance.visualInput;
        }
        
        public static void SetVisualInput<TValueType>(this BaseField<TValueType> instance, VisualElement visualInput)
        {
            instance.visualInput = visualInput;
        }
    }
}