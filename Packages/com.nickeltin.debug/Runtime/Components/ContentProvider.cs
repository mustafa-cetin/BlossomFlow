using UnityEngine;

namespace nickeltin.DebugPanel.Runtime
{
    /// <summary>
    /// Inherit from this class to create a custom content for <see cref="DebugPanel"/>
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public abstract class ContentProvider : MonoBehaviour, IContentProvider
    {
        public virtual IInputModule GetInputModule(DebugPanel panel) => null;
        
        protected virtual void OnValidate()
        {
            NotifyDebugPanel();
        }

        protected virtual void OnDisable()
        {
            NotifyDebugPanel();
        }

        public void NotifyDebugPanel()
        {
            var debugPanel = gameObject.GetComponentInParent<DebugPanel>();
            if (debugPanel != null) debugPanel.SetDirty();
        }

        protected void Reset()
        {
            NotifyDebugPanel();
        }

        public abstract void PopulateContent(DebugPanel panel);
        
        /// <inheritdoc cref="IContentProvider.PopulateHorizontalContent"/>
        /// <remarks-impl>
        /// By default, calls for <see cref="PopulateContent"/>
        /// </remarks-impl>
        public virtual void PopulateHorizontalContent(DebugPanel panel) => PopulateContent(panel);

        /// <inheritdoc cref="IContentProvider.PopulateVerticalContent"/>
        /// <remarks-impl>
        /// By default, calls for <see cref="PopulateContent"/>
        /// </remarks-impl>
        public virtual void PopulateVerticalContent(DebugPanel panel) => PopulateContent(panel);
    }
}