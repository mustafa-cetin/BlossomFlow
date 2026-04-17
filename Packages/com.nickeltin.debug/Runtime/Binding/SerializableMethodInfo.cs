using System;

namespace nickeltin.DebugPanel.Runtime.Binding
{
    [Serializable]
    internal struct SerializableMethodInfo
    {
        public string Assembly;
        public string Type;
        public string Method;
    }
}