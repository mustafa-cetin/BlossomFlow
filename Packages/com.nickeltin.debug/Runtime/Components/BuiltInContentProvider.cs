using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    /// <summary>
    /// Exist more of a showcase and default option, It's recommended to create custom <see cref="ContentProvider"/>
    /// for your game needs, and populate content there
    /// </summary>
    public class BuiltInContentProvider : ContentProvider
    {
        internal class EmptyImplementation : IContentProvider
        {
            public static EmptyImplementation Instance { get; } = new EmptyImplementation();
            
            public IInputModule GetInputModule(DebugPanel panel) => null;
            
            public void PopulateContent(DebugPanel panel)
            {
                var label = new UnityEngine.UIElements.Label(
                    $"This is default content provider, add custom {nameof(ContentProvider)} to define UI");
                label.style.whiteSpace = WhiteSpace.Normal;
                panel.AddElementToCurrentGroup(label);
            }

            public void PopulateHorizontalContent(DebugPanel panel) => PopulateContent(panel);

            public void PopulateVerticalContent(DebugPanel panel) => PopulateContent(panel);
        }
        
        internal interface IContent
        {
            void PopulateContent(DebugPanel panel);

            void Begin(DebugPanel panel);
            
            void End(DebugPanel panel);
            
            IEnumerable<IContent> GetContent();
        }

        internal abstract class ContentBase : IContent
        {
            public virtual void PopulateContent(DebugPanel panel) { }

            public virtual void Begin(DebugPanel panel) { }

            public virtual void End(DebugPanel panel) { }

            public virtual IEnumerable<IContent> GetContent()
            {
                yield break;
            }
        }

        [Serializable]
        internal abstract class GroupBase : ContentBase
        {
            public string Name;
            
            [SerializeReference]
            public List<IContent> Content =  new();
            
            public abstract override void Begin(DebugPanel panel);
            public abstract override void End(DebugPanel panel);

            public override IEnumerable<IContent> GetContent() => Content;
        }
        
        [Serializable]
        internal class TabGroup : GroupBase
        {
            public TabGroup(string name) => Name = name;

            public override void Begin(DebugPanel panel) => panel.BeginTabGroup(Name);

            public override void End(DebugPanel panel) => panel.EndTabGroup();
        }
        
        [Serializable]
        internal class Group : GroupBase
        {
            public bool Expanded;
            public GroupOptions Options;

            public Group(string name, GroupOptions options)
            {
                Name = name;
                Options = options;
            }

            public override void Begin(DebugPanel panel)
            {
                if (string.IsNullOrEmpty(Name))
                {
                    panel.BeginGroup(Options, Expanded);
                }
                else
                {
                    panel.BeginGroup(Options, Name, Expanded);
                }
            }

            public override void End(DebugPanel panel)
            {
                panel.EndGroup();
            }
        }

        [Serializable]
        internal abstract class ElementBase : ContentBase
        {
            public string Text;

            protected ElementBase(string text) => Text = text;
            
            public abstract override void PopulateContent(DebugPanel panel);
        } 
        
        [Serializable]
        internal class Label : ElementBase
        {
            public Label(string text) : base(text) { }

            public override void PopulateContent(DebugPanel panel) => panel.AddLabel(Text);
        }
        
        [Serializable]
        internal class Button : ElementBase
        {
            public bool IsRepeat;
            public UnityEvent OnClick;

            public Button(string text) :  base(text) { }

            public override void PopulateContent(DebugPanel panel)
            {
                if (IsRepeat) panel.AddRepeatButton(Text, OnClick.Invoke);
                else panel.AddButton(Text, OnClick.Invoke);
            }
        }

        [Serializable]
        internal class Toggle : ElementBase
        {
            public bool DefaultValue;
            public UnityEvent<bool> OnValueChanged;
            
            public Toggle(string text, bool defaultValue = false) : base(text)
            {
                DefaultValue = defaultValue;
            }

            public override void PopulateContent(DebugPanel panel)
            {
                panel.AddToggle(Text, DefaultValue, OnValueChanged.Invoke);
            }
        }

        [Serializable]
        internal class Dropdown : ElementBase
        {
            public List<string> Options;
            public UnityEvent<int> OnValueChanged;

            public Dropdown(string text, List<string> options = null) : base(text) => Options = options;

            public override void PopulateContent(DebugPanel panel)
            {
                panel.AddDropdown(Text, () => Options, OnValueChanged.Invoke);
            }
        }

        [Serializable]
        internal class Layout
        {
            [SerializeReference] public List<IContent> MainTabContent;
            public List<TabGroup> AdditionalTabs;
        }
        
        [SerializeField] internal Layout _verticalLayout = CreateDefaultVerticalLayout();
        [SerializeField] internal Layout _horizontalLayout = CreateDefaultHorizontalLayout();
        
        public override void PopulateContent(DebugPanel panel)
        {
            PopulateVerticalContent(panel);
        }

        public override void PopulateHorizontalContent(DebugPanel panel)
        {
            PopulateLayout(_horizontalLayout, panel);
        }

        public override void PopulateVerticalContent(DebugPanel panel)
        {
            PopulateLayout(_verticalLayout, panel);
        }

        private static void PopulateLayout(Layout layout, DebugPanel panel)
        {
            PopulateContents(layout.AdditionalTabs, panel);
            PopulateContents(layout.MainTabContent, panel);
        }

        private static void PopulateContents(IEnumerable<IContent> groups, DebugPanel panel)
        {
            foreach (var group in groups) PopulateContent(group, panel);
        }
        
        private static void PopulateContent(IContent content, DebugPanel panel)
        {
            if (content == null) return;
            
            content.Begin(panel);
            content.PopulateContent(panel);
            foreach (var childContent in content.GetContent()) PopulateContent(childContent, panel);
            content.End(panel);
        }

        private static Layout CreateDefaultVerticalLayout()
        {
            var defaultDropdownOptions = new List<string>()
            {
                "Option1",
                "Option2",
                "Option3",
                "Option4"
            };
            
            var content = new List<IContent>()
            {
                new Group("Group1(Vertical)", GroupOptions.Vertical)
                {
                    Content = new List<IContent>()
                    {
                        new Group("Group2(Horizontal)", GroupOptions.Horizontal)
                        {
                            Options = GroupOptions.Horizontal,
                            Content = new List<IContent>()
                            {
                                new Button("Button1"),
                                new Button("Button2"),
                                new Button("Button3")
                            }
                        },
                        new Group("Group3(Horizontal, Expandable)", GroupOptions.Horizontal | GroupOptions.Expandable)
                        {
                            Expanded = true,
                            Content = new List<IContent>()
                            {
                                new Label("Label1"),
                                new Label("Label2"),
                                new Label("Label3")
                            }
                        },
                        new  Group("Group4(Horizontal, Borderless)", GroupOptions.Horizontal | GroupOptions.Borderless)
                        {
                            Content = new List<IContent>()
                            {
                                new Toggle("Toggle1", true),
                                new Toggle("Toggle2", false),
                                new Toggle("Toggle3", true),
                            }
                        },
                        new  Group("Group5(Vertical)", GroupOptions.Vertical)
                        {
                            Content = new List<IContent>()
                            {
                                new Dropdown("Dropdown1", defaultDropdownOptions),
                                new Dropdown("Dropdown2", defaultDropdownOptions),
                                new Dropdown("Dropdown3", defaultDropdownOptions),
                            }
                        },
                        new  Group("Group6(Horizontal, Borderless, Expandable)", GroupOptions.Horizontal | GroupOptions.Borderless |  GroupOptions.Expandable)
                        {
                            Content = new List<IContent>()
                            {
                                new Button("RepeatButton1")
                                {
                                    IsRepeat = true
                                },
                                new Button("RepeatButton2")
                                {
                                    IsRepeat = true
                                },
                                new Button("RepeatButton3")
                                {
                                    IsRepeat = true
                                },
                            }
                        }
                        
                    }
                }
            };

            return new Layout
            {
                MainTabContent = content,
                AdditionalTabs = new List<TabGroup>()
            };
        }
        
        private static Layout CreateDefaultHorizontalLayout()
        {
            var defaultDropdownOptions = new List<string>()
            {
                "Option1",
                "Option2",
                "Option3",
                "Option4"
            };
            
            var mainTabContent = new List<IContent>()
            {
                new Group("Group1(Horizontal)", GroupOptions.Horizontal)
                {
                    Content = new List<IContent>()
                    {
                        new Group("", GroupOptions.Vertical | GroupOptions.Borderless)
                        {
                            Content = new List<IContent>()
                            {
                                new Group("Group2(Horizontal)", GroupOptions.Horizontal)
                                {
                                    Options = GroupOptions.Horizontal,
                                    Content = new List<IContent>()
                                    {
                                        new Button("Button1"),
                                        new Button("Button2"),
                                        new Button("Button3")
                                    }
                                },
                                new Group("Group3(Vertical, Expandable)", GroupOptions.Vertical | GroupOptions.Expandable)
                                {
                                    Expanded = true,
                                    Content = new List<IContent>()
                                    {
                                        new Label("Label1"),
                                        new Label("Label2"),
                                        new Label("Label3")
                                    }
                                },
                            }
                        },
                        new Group("", GroupOptions.Vertical | GroupOptions.Borderless)
                        {
                            Content = new List<IContent>()
                            {
                                new  Group("Group4(Vertical, Borderless)", GroupOptions.Vertical | GroupOptions.Borderless)
                                {
                                    Content = new List<IContent>()
                                    {
                                        new Toggle("Toggle1", true),
                                        new Toggle("Toggle2", false),
                                        new Toggle("Toggle3", true),
                                    }
                                },
                                new  Group("Group5(Vertical)", GroupOptions.Vertical)
                                {
                                    Content = new List<IContent>()
                                    {
                                        new Dropdown("Dropdown1", defaultDropdownOptions),
                                        new Dropdown("Dropdown2", defaultDropdownOptions),
                                        new Dropdown("Dropdown3", defaultDropdownOptions),
                                    }
                                }
                            }
                        },
                    }
                }
            };

            var additionalTabs = new List<TabGroup>()
            {
                new("Horizontal Extended Group")
                {
                    Content = new List<IContent>()
                    {
                        new Label("This is tab exclusive for horizontal layout")
                    }
                }
            };
            
            return new Layout
            {
                MainTabContent = mainTabContent,
                AdditionalTabs = additionalTabs
            };
        }
    }
}