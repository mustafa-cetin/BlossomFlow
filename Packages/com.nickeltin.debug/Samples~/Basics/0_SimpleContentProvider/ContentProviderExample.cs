using System.Collections.Generic;
using System.Globalization;
using nickeltin.DebugPanel.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Examples.Runtime
{
    /// <summary>
    /// Just an example class showing how to fill <see cref="DebugPanel"/> with content.
    /// To create your own <see cref="ContentProvider"/> inherit from base class and then fill <see cref="DebugPanel"/> as showed here.
    /// Attaching your Provider component to GameObject will allow to add <see cref="DebugPanel"/> itself.
    /// </summary>
    public class ContentProviderExample : ContentProvider
    {
        [SerializeField] private bool _toggleState1 = true;
        [SerializeField] private bool _toggleState2 = false;
        [SerializeField] private List<string> _dropdownOptions = new List<string>()
        {
            "Regular", "Top", "Right", "Left"
        };

        private float _repeatButtonNumber;
        private Label _repeatButtonLabel;
        
        /// <summary>
        /// Two options for creating presented here. 
        /// </summary>
        public override void PopulateContent(DebugPanel.Runtime.DebugPanel panel)
        {
            // Regular methods 
            // BeginGroup need to be called manually at the start and EndGroup at the end
            panel.BeginGroup(GroupOptions.Vertical, "Resources");
            {
                // Regular horizontal group
                panel.BeginGroup(GroupOptions.Horizontal, "Coal");
                {
                    panel.AddButton("+1", () => Debug.Log(1));
                    panel.AddButton("+5", () => Debug.Log(5));
                    panel.AddButton("+10", () => Debug.Log(10));
                    panel.AddButton("+20", () => Debug.Log(20));
                    panel.AddButton("+50", () => Debug.Log(50));
                }
                panel.EndGroup();

                // Horizontal group with Toggle arrow
                panel.BeginGroup(GroupOptions.Horizontal | GroupOptions.Expandable, "Coins");
                {
                    panel.AddButton("+10", () => Debug.Log(10));
                    panel.AddButton("+50", () => Debug.Log(50));
                    panel.AddButton("+100", () => Debug.Log(10));
                    panel.AddButton("+500", () => Debug.Log(500));
                }
                panel.EndGroup();
                
                // Horizontal group with disabled borders and padding
                panel.BeginGroup(GroupOptions.Horizontal | GroupOptions.Borderless, "Exp");
                {
                    panel.AddButton("+100", () => Debug.Log(100));
                    panel.AddButton("+500", () => Debug.Log(500));
                    panel.AddButton("+1000", () => Debug.Log(1000));
                    panel.AddButton("+5000", () => Debug.Log(5000));
                }
                panel.EndGroup();

                // Label field
                panel.AddLabel("LABEL");

                // Repeat buttons
                using (new Group(panel, GroupOptions.Horizontal, "Repeat buttons"))
                {
                    panel.AddRepeatButton("Minus", () =>
                    {
                        _repeatButtonNumber--;
                        _repeatButtonLabel.text = _repeatButtonNumber.ToString(CultureInfo.InvariantCulture);
                    });

                    _repeatButtonLabel = panel.AddLabel("0");

                    panel.AddRepeatButton("Plus", () =>
                    {
                        _repeatButtonNumber++;
                        _repeatButtonLabel.text = _repeatButtonNumber.ToString(CultureInfo.InvariantCulture);
                    });
                }

                // Toggle fields
                panel.AddToggle("Toggle 1", _toggleState1, b => _toggleState1 = b);
                panel.AddToggle("Toggle 2", _toggleState2, b => _toggleState2 = b);
            }
            panel.EndGroup();

            panel.AddDropdown("Camera", () => _dropdownOptions, i =>
            {
                Debug.Log("Value changed! " + _dropdownOptions[i]);
            });
            
            
            // Helper structs and using block
            // in this case .EndGroup/.EndTabGroup will be called at the end of using block
            using (new TabGroup(panel, "COMPLEX COMMANDS"))
            {
                using (new Group(panel, GroupOptions.Horizontal,"Messages"))
                {
                    panel.AddButton("Log", () => 
                        Debug.Log("lollllllllllllllllllllllasdasdasdasddddddddddddddddddddddddddddddddddddasdddddddddddddddddddddasdsdsdsdsdsdsdsd."));
                    panel.AddButton("Warning", () => 
                        Debug.LogWarning("Kekasddddddddddddddddddddddddddasdasasasasasasasasasasasasasasasasasasasasasasasasasasasasasasasas."));
                    panel.AddButton("Error", () => 
                        Debug.LogError("Chebureksdasdadasdddddddddddddddddddddddddddddasddddddddddddddddddddddddddddddddddd."));
                }
            }
        }
    }
}