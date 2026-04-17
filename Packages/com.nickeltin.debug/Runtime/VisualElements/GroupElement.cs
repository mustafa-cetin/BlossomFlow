using System.Linq;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    public class GroupElement : VisualElement
    {
        private readonly VisualElement _contentContainer;
        private readonly Foldout _expander;
        private readonly ScrollViewElement _scrollView;
        private GroupOptions _options;

        public TabElement Tab { get; private set; }

        public bool Expanded
        {
            get => _expander != null && _expander.value;
            set
            {
                if (_expander != null) _expander.value = value;
            }
        }

        internal GroupElement(GroupOptions options, bool useScrollView, DebugPanelElementsFactory factory)
        {
            _options = options;
            name = "Group";
            
            AddToClassList(DebugPanelStyleClasses.group);
            
            _contentContainer = new VisualElement { name = "ContentContainer" };

            if (options.HasFlag(GroupOptions.Expandable))
            {
                AddToClassList(DebugPanelStyleClasses.expandable);
                _expander = new Foldout { name = "Expander" };
                _expander.AddToClassList(DebugPanelStyleClasses.expander);
                Add(_expander);
                _expander.contentContainer.Add(_contentContainer);
            }
            else
            {
                Add(_contentContainer);
            }
            

            if (options.HasFlag(GroupOptions.Vertical))
            {
                _contentContainer.AddToClassList(DebugPanelStyleClasses.verticalGroup);
            }

            if (options.HasFlag(GroupOptions.Horizontal))
            {
                _contentContainer.AddToClassList(DebugPanelStyleClasses.horizontalGroup);
            }

            if (options.HasFlag(GroupOptions.Borderless))
            {
                AddToClassList(DebugPanelStyleClasses.borderless);   
            }

            if (useScrollView)
            {
                _scrollView = factory.CreateScrollView();
                Add(_scrollView);
                _scrollView.contentContainer.Add(_contentContainer);
            }
        }

        internal void AddLabel(string label)
        {
            if (!_options.HasFlag(GroupOptions.Expandable))
            {
                Insert(0, new Label(label)
                {
                    name = "Label"
                });
            }
            else
            {
                _expander.text = label;
            }

            name += label;
        }
        
        public void AddContent(VisualElement visualElement)
        {
            _contentContainer.Add(visualElement);
        }

        internal static void SetTab(GroupElement group, TabElement tab) => group.Tab = tab;

        internal static GroupElement GetFromTab(TabElement tab, VisualElement groupsRoot)
        {
            return groupsRoot.Query<GroupElement>().Build().FirstOrDefault(g => g.Tab == tab);
        }
    }
}