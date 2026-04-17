using UnityEngine;
using UnityEngine.UIElements;
using nickeltin.DebugPanel.Runtime;
using DPanel = nickeltin.DebugPanel.Runtime.DebugPanel;

namespace nickeltin.DebugPanel.Examples.Runtime
{
    /// <summary>
    /// Example demonstrating how to track and display game state in real-time using Labels.
    /// This connects to an existing GateSystemManager to display its state.
    /// 
    /// Features:
    /// - Real-time state tracking with Labels
    /// - Connects to independent game system
    /// - Event-driven UI updates
    /// </summary>
    public class GameStateTrackingContentProviderExample : ContentProvider
    {
        // Reference to the independent game system
        private GateSystemManager _systemManager;
        private GateSystem _gateSystem;

        // UI Labels for tracking state
        private Label _gateHealthLabel;
        private Label _gateStatusLabel;
        private Label _enemyCountLabel;
        private Label _damageReceivedLabel;
        private Label _gatePositionLabel;
        private Label _enemyDamageRateLabel;
        private Label _gameStateLabel;

        private void Start()
        {
            // Find the existing gate system in the scene
            _systemManager = GateSystemManager.Instance;

            if (_systemManager == null)
            {
                Debug.LogWarning("GateSystemManager not found in scene! Please add the GateDefenseSystem prefab.");
                return;
            }

            _gateSystem = _systemManager.GateSystem;

            // Subscribe to state changes
            if (_gateSystem != null) _gateSystem.OnStateChanged += RefreshAllLabels;
        }

        private void Update()
        {
            if (_gateSystem == null) return;

            // Update position label every frame to show real-time tracking
            if (_gatePositionLabel != null) _gatePositionLabel.text = $"Position: {_gateSystem.Position:F2}";
        }

        private void OnDestroy()
        {
            if (_gateSystem != null) _gateSystem.OnStateChanged -= RefreshAllLabels;
        }

        /// <summary>
        /// Refresh all labels with current gate system data
        /// </summary>
        private void RefreshAllLabels()
        {
            if (_gateSystem == null) return;

            if (_gateHealthLabel != null)
            {
                var healthPercent = _gateSystem.HealthPercentage * 100f;
                var healthColor = healthPercent > 60f ? "green" : healthPercent > 30f ? "yellow" : "red";
                _gateHealthLabel.text =
                    $"Health: <color={healthColor}>{_gateSystem.Health:F0}/{_gateSystem.MaxHealth:F0} ({healthPercent:F0}%)</color>";
            }

            if (_gateStatusLabel != null)
            {
                var statusColor = _gateSystem.IsOpen ? "green" : "red";
                _gateStatusLabel.text =
                    $"Status: <color={statusColor}>{(_gateSystem.IsOpen ? "OPEN" : "CLOSED")}</color>";
            }

            if (_enemyCountLabel != null)
            {
                var countColor = _gateSystem.EnemyCount > 0 ? "orange" : "white";
                _enemyCountLabel.text = $"Enemies Nearby: <color={countColor}>{_gateSystem.EnemyCount}</color>";
            }

            if (_damageReceivedLabel != null)
                _damageReceivedLabel.text = $"Total Damage: {_gateSystem.TotalDamageReceived:F1}";

            if (_enemyDamageRateLabel != null)
            {
                var totalDPS = _gateSystem.EnemyCount * _gateSystem.DamagePerEnemyPerSecond;
                var dpsColor = totalDPS > 5f ? "red" : totalDPS > 0 ? "orange" : "white";
                _enemyDamageRateLabel.text = $"Enemy DPS: <color={dpsColor}>{totalDPS:F1}/sec</color>";
            }

            if (_gameStateLabel != null)
            {
                var stateText = _gateSystem.CurrentGameState.ToString().ToUpper();
                var stateColor = _gateSystem.CurrentGameState switch
                {
                    GameState.Idle => "white",
                    GameState.Active => "yellow",
                    GameState.Defeated => "red",
                    GameState.Victory => "green",
                    _ => "white"
                };
                _gameStateLabel.text = $"Game State: <color={stateColor}>{stateText}</color>";
            }
        }

        public override void PopulateContent(DPanel panel)
        {
            if (_gateSystem == null)
            {
                panel.AddLabel("ERROR: Gate system not found!");
                panel.AddLabel("Add GateDefenseSystem prefab to scene and enter playmode");
                return;
            }

            using (new Group(panel, GroupOptions.Horizontal | GroupOptions.Borderless))
            {
                // Left column - Status display
                using (new Group(panel, GroupOptions.Vertical, "Status"))
                {
                    _gameStateLabel = panel.AddLabel("Game State");
                    _gateStatusLabel = panel.AddLabel("Status");
                    _gatePositionLabel = panel.AddLabel("Position");
                    _damageReceivedLabel = panel.AddLabel("Total Damage");
                    _enemyCountLabel = panel.AddLabel("Enemies Nearby");
                    _enemyDamageRateLabel = panel.AddLabel("Enemy DPS");

                    // Initial refresh
                    RefreshAllLabels();

                    panel.AddToggle("Enemies Attack", _gateSystem.EnemiesCanAttack, (newValue) =>
                    {
                        _gateSystem.EnemiesCanAttack = newValue;
                        Debug.Log($"Enemies attack: {newValue}");
                    });
                }

                // Middle column - Health
                using (new Group(panel, GroupOptions.Vertical, "Health"))
                {
                    _gateHealthLabel = panel.AddLabel("Health: 100/100 (100%)");

                    panel.AddButton("-10", () => _gateSystem.TakeDamage(10f));
                    panel.AddButton("-50", () => _gateSystem.TakeDamage(50f));
                    panel.AddButton("+100", () => _gateSystem.Repair(100f));
                }

                // Right column - Gate & Enemies
                using (new Group(panel, GroupOptions.Vertical, "Controls"))
                {
                    panel.AddButton("Open Gate", () =>
                    {
                        _gateSystem.Open();
                        Debug.Log("Gate opened");
                    });

                    panel.AddButton("Close Gate", () =>
                    {
                        _gateSystem.Close();
                        Debug.Log("Gate closed");
                    });
                    
                    panel.AddButton("+1 Enemy", () => _gateSystem.AddEnemy());
                    panel.AddButton("-1 Enemy", () => _gateSystem.RemoveEnemy());
                    panel.AddButton("+5 Enemies", () =>
                    {
                        for (var i = 0; i < 5; i++) _gateSystem.AddEnemy();
                    });

                    panel.AddButton("Clear All Enemies", () =>
                    {
                        while (_gateSystem.EnemyCount > 0) _gateSystem.RemoveEnemy();
                    });

                    panel.AddButton("Reset System", () =>
                    {
                        if (_systemManager != null)
                        {
                            _systemManager.ResetSystem();
                            Debug.Log("Gate system reset");
                        }
                    });
                }
                
            }
            
            // After all content is populated labels state can be updated
            RefreshAllLabels();
        }
    }
}