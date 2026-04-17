using UnityEngine;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    /// <summary>
    /// Extension methods for ScrollView to add auto-scroll and focus behavior.
    /// </summary>
    public static class ScrollViewExtensions
    {
        /// <summary>
        /// Configures the scroller to make only the slider non-focusable, keeping the scroll view purely visual.
        /// </summary>
        public static void ConfigureScrollerFocusability(this ScrollView scrollView)
        {
            // Get the vertical scroller
            var scroller = scrollView.Q<Scroller>(className: "unity-scroller--vertical");
            // Make the scroller slider (dragger) not focusable
            var slider = scroller?.slider;
            
            if (slider == null) return;
            
            slider.focusable = false;
        }
        
        /// <summary>
        /// Scrolls the view to center the specified element in the viewport.
        /// </summary>
        public static void ScrollToCenter(this ScrollView scrollView, VisualElement targetElement)
        {
            if (targetElement == null)
                return;

            // Get the position of the target element relative to the ScrollView's content container
            // We need to transform coordinates because the element might be nested deep in the hierarchy
            var targetWorldBound = targetElement.worldBound;
            var contentWorldBound = scrollView.contentContainer.worldBound;
            
            // Calculate the relative position
            var targetRelativePosition = new Vector2(
                targetWorldBound.x - contentWorldBound.x,
                targetWorldBound.y - contentWorldBound.y
            );
            
            var targetSize = targetElement.layout.size;
            
            // Get the size of the ScrollView's viewport
            var viewportSize = scrollView.contentViewport.layout.size;
            
            // Calculate the desired scroll offset to center the target element
            // We want: targetPosition + (targetSize / 2) - (viewportSize / 2)
            var desiredScrollOffset = new Vector2(
                targetRelativePosition.x + (targetSize.x / 2f) - (viewportSize.x / 2f),
                targetRelativePosition.y + (targetSize.y / 2f) - (viewportSize.y / 2f)
            );
            
            // Clamp the scroll offset to valid bounds (don't scroll beyond content)
            desiredScrollOffset.x = Mathf.Max(0, desiredScrollOffset.x);
            desiredScrollOffset.y = Mathf.Max(0, desiredScrollOffset.y);
            
            // Set the ScrollView's scrollOffset to the calculated value
            scrollView.scrollOffset = desiredScrollOffset;
        }
        
        /// <summary>
        /// Registers a callback to automatically scroll to center-focused elements within this ScrollView.
        /// </summary>
        public static void RegisterAutoScrollToCenter(this ScrollView scrollView)
        {
            scrollView.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                // if (evt.direction == Naviga)
                
                var focusedElement = evt.target as VisualElement;
                if (focusedElement == null)
                    return;

                // Check if the focused element is within the content container
                if (!scrollView.contentContainer.Contains(focusedElement))
                    return;

                // Schedule scrolling after layout update to ensure accurate positions
                scrollView.schedule.Execute(() =>
                { 
                    // Debug.Log("Navigation move event, scrolling to center");
                    scrollView.ScrollToCenter(focusedElement);
                });
            }, TrickleDown.TrickleDown);
        }
    }
}