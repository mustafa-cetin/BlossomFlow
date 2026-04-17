using nickeltin.DebugPanel.Runtime;

namespace nickeltin.DebugPanel.Examples.Runtime
{
    /// <summary>
    /// In this example content is created only for static bindings, all buttons are created from a static context.
    /// It's useful to set up some templates for groups,
    /// else all buttons created from bindings will create groups with default settings. 
    /// </summary>
    public class BindingsContentProviderExample : ContentProvider
    {
        public override void PopulateContent(DebugPanel.Runtime.DebugPanel panel)
        {
            using (new Group(panel, GroupOptions.Expandable, "Player"))
            {
                using (new Group(panel, GroupOptions.Horizontal, "Heal"))
                {
                    
                }
            }
            
            using (new Group(panel, GroupOptions.Expandable, "Mobs"))
            {
                using (new Group(panel, GroupOptions.Horizontal, "Spawn"))
                {
                   
                }
                
                using (new Group(panel, GroupOptions.Horizontal, "Despawn"))
                {
                   
                }
            }

            using (new TabGroup(panel, "Secret Bindings"))
            {
                using (new Group(panel, GroupOptions.Vertical, "Player"))
                {
                    
                }
            }
        }
    }
}
