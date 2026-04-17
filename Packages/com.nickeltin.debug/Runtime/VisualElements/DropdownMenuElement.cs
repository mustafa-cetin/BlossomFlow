using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    public class DropdownMenuElement : GenericDropdownMenu
    {
        public readonly VisualElement visualInputOverride;

        public DropdownMenuElement(VisualElement visualInputOverride)
        {
            this.visualInputOverride = visualInputOverride;
        }
    }
}