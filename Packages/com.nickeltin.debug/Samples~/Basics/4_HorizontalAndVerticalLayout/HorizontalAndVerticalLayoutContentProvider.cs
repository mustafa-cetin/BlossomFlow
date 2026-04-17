using UnityEngine;
using nickeltin.DebugPanel.Runtime;
using DPanel = nickeltin.DebugPanel.Runtime.DebugPanel;

namespace nickeltin.DebugPanel.Examples.Runtime
{
    /// <summary>
    /// Horizontal/Vertical mode lets you define different content for a debug panel based on the current screen aspect ratio.
    /// This is useful when the application supports different orientations (Portrait, Landscape) 
    /// </summary>
    public class HorizontalAndVerticalLayoutContentProvider : ContentProvider
    {
        public override void PopulateContent(DPanel panel)
        {
            PopulateVerticalContent(panel);
        }

        public override void PopulateVerticalContent(DPanel panel)
        {
            // For vertical layout we'll use the default populate content method
            PopulateVerticalContentMainTab(panel);

            // Add debug tab - present in all layouts
            PopulateDebugTab(panel);
        }

        private void PopulateVerticalContentMainTab(DPanel panel)
        {
            // Game controls section - compact for vertical layout
            using (new Group(panel, GroupOptions.Vertical | GroupOptions.Expandable, "Game Controls"))
            {
                panel.AddButton("Start Game", () => Debug.Log("Game Started"));
                panel.AddButton("Pause Game", () => Debug.Log("Game Paused"));
                panel.AddButton("Restart Level", () => Debug.Log("Level Restarted"));
            }

            // Quick settings - vertical friendly
            using (new Group(panel, GroupOptions.Vertical, "Settings"))
            {
                panel.AddToggle("Sound Effects", true, b => Debug.Log($"SFX: {b}"));
                panel.AddToggle("Music", true, b => Debug.Log($"Music: {b}"));
                panel.AddToggle("Vibration", false, b => Debug.Log($"Vibration: {b}"));
            }

            // Time scale controls
            using (new Group(panel, GroupOptions.Horizontal, "Time Scale"))
            {
                panel.AddButton("0.5x", () => Time.timeScale = 0.5f);
                panel.AddButton("1x", () => Time.timeScale = 1f);
                panel.AddButton("2x", () => Time.timeScale = 2f);
            }
        }

        private void PopulateDebugTab(DPanel panel)
        {
            // Debug tab - present in all layouts
            using (new TabGroup(panel, "DEBUG"))
            {
                using (new Group(panel, GroupOptions.Vertical, "Resources"))
                {
                    panel.AddButton("+100 Coins", () => Debug.Log("Added 100 coins"));
                    panel.AddButton("+1000 Coins", () => Debug.Log("Added 1000 coins"));
                    panel.AddButton("Max Health", () => Debug.Log("Health maxed"));
                }

                using (new Group(panel, GroupOptions.Vertical | GroupOptions.Expandable, "Logging"))
                {
                    panel.AddButton("Log Message", () => Debug.Log("Test log message"));
                    panel.AddButton("Warning", () => Debug.LogWarning("Test warning"));
                    panel.AddButton("Error", () => Debug.LogError("Test error"));
                }
            }
        }

        public override void PopulateHorizontalContent(DPanel panel)
        {
            // For horizontal layout we'll wrap vertical content in "two-column view"
            using (new Group(panel, GroupOptions.Horizontal | GroupOptions.Borderless))
            {
                // For first column just use default vertical layout from default tab
                using (new Group(panel, GroupOptions.Vertical | GroupOptions.Borderless))
                {
                    PopulateVerticalContentMainTab(panel);
                }

                // For the second column made horizontal layout only additions 
                using (new Group(panel, GroupOptions.Vertical | GroupOptions.Borderless))
                {
                    // Advanced options that appear only in horizontal/landscape mode
                    using (new Group(panel, GroupOptions.Vertical, "Advanced"))
                    {
                        panel.AddButton("Clear PlayerPrefs", () =>
                        {
                            PlayerPrefs.DeleteAll();
                            Debug.Log("PlayerPrefs cleared");
                        });
                        panel.AddButton("Reload Scene", () =>
                        {
                            UnityEngine.SceneManagement.SceneManager.LoadScene(
                                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                        });
                    }

                    // Graphics settings
                    using (new Group(panel, GroupOptions.Vertical | GroupOptions.Expandable, "Graphics"))
                    {
                        panel.AddToggle("VSync", QualitySettings.vSyncCount > 0,
                            b => QualitySettings.vSyncCount = b ? 1 : 0);
                        
                        panel.AddDropdown("Quality Level", 
                            () => new System.Collections.Generic.List<string>(QualitySettings.names),
                            index =>
                            {
                                QualitySettings.SetQualityLevel(index);
                                Debug.Log($"Quality set to: {QualitySettings.names[index]}");
                            },
                            QualitySettings.GetQualityLevel());
                    }
                }
            }

            // Add debug tab - present in all layouts
            PopulateDebugTab(panel);

            // And also add another tab
            using (new TabGroup(panel, "DEVELOPER TOOLS"))
            {
                // Two-column layout for developer tools
                using (new Group(panel, GroupOptions.Horizontal | GroupOptions.Borderless))
                {
                    // Left column - Performance
                    using (new Group(panel, GroupOptions.Vertical, "Performance"))
                    {
                        panel.AddButton("Show FPS", () => Debug.Log("FPS Counter toggled"));
                        panel.AddButton("Profile Frame", () => Debug.Log("Frame profiled"));
                        panel.AddToggle("Draw Gizmos", false, b => Debug.Log($"Gizmos: {b}"));
                    }

                    // Right column - Testing
                    using (new Group(panel, GroupOptions.Vertical, "Testing"))
                    {
                        panel.AddButton("Spawn Enemy", () => Debug.Log("Enemy spawned"));
                        panel.AddButton("Clear All Enemies", () => Debug.Log("Enemies cleared"));
                        panel.AddButton("Trigger Win", () => Debug.Log("Win triggered"));
                        panel.AddButton("Trigger Lose", () => Debug.Log("Lose triggered"));
                    }
                }

                // Full width section
                using (new Group(panel, GroupOptions.Horizontal, "Quick Actions"))
                {
                    panel.AddButton("Screenshot", () =>
                        ScreenCapture.CaptureScreenshot("screenshot.png"));
                    panel.AddButton("Toggle Console", () => Debug.Log("Console toggled"));
                    panel.AddButton("Reset All", () => Debug.Log("Everything reset"));
                }
            }
        }
    }
}