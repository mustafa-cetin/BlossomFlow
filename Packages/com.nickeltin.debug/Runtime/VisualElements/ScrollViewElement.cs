using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    /// <summary>
    /// A ScrollView that automatically scrolls to bring focused elements into view.
    /// </summary>
    public class ScrollViewElement : ScrollView
    {
        public ScrollViewElement() : base(ScrollViewMode.Vertical)
        {
            // Register auto-scroll behavior
            this.RegisterAutoScrollToCenter();
            
            // Configure scroll controls focusability after the element is added to the hierarchy
            RegisterCallback<AttachToPanelEvent>(evt => schedule.Execute(this.ConfigureScrollerFocusability));
        }
    }
}

