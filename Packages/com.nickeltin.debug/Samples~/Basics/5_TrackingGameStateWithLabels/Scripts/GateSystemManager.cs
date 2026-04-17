using UnityEngine;

namespace nickeltin.DebugPanel.Examples.Runtime
{
    /// <summary>
    /// Manages the gate defense system independently from the debug panel.
    /// This is the core game system that exists regardless of debug UI.
    /// </summary>
    public class GateSystemManager : MonoBehaviour
    {
        public static GateSystemManager Instance { get; private set; }

        [Header("Visual Components")]
        [SerializeField] private GateVisualizer gateVisualizer;
        [SerializeField] private EnemyVisualizer enemyVisualizer;

        // Core gate system
        private GateSystem _gateSystem;
        public GateSystem GateSystem => _gateSystem;

        private void Awake()
        {
            // Singleton pattern for easy access
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize the core system
            _gateSystem = new GateSystem();

            // Initialize visualizers if assigned
            if (gateVisualizer != null)
            {
                gateVisualizer.Initialize(_gateSystem);
            }

            if (enemyVisualizer != null)
            {
                enemyVisualizer.Initialize(_gateSystem);
            }
        }

        private void Update()
        {
            if (_gateSystem != null)
            {
                _gateSystem.Update(Time.deltaTime);
            }
        }

        /// <summary>
        /// Reset the entire gate system to initial state
        /// </summary>
        public void ResetSystem()
        {
            if (_gateSystem != null)
            {
                _gateSystem.Reset();
            }

            // Reset visualizers
            if (gateVisualizer != null)
            {
                gateVisualizer.ResetState();
            }

            if (enemyVisualizer != null)
            {
                enemyVisualizer.ResetState();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}

