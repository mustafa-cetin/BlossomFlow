using System;

namespace nickeltin.DebugPanel.Runtime
{
    internal struct DynamicValue<T>
    {
        public delegate T GetterDelegate();
        public delegate void SetterDelegate(T value);

        public event Action Changed;
        
        private readonly GetterDelegate _getter;
        private readonly SetterDelegate _setter;

        public T Value
        {
            get => _getter();
            set => _setter(value);
        }

        public DynamicValue(GetterDelegate getter, SetterDelegate setter) : this()
        {
            _getter = getter;
            _setter = setter;
        }

        public void InvokeValueChanged() => Changed?.Invoke();
    }
}