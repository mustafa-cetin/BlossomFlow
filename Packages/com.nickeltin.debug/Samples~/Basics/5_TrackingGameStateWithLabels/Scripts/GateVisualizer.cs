using UnityEngine;

namespace nickeltin.DebugPanel.Examples.Runtime
{
    /// <summary>
    /// Visualizes the gate in 3D space with animated opening/closing and health-based color
    /// </summary>
    public class GateVisualizer : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private Transform leftDoor;
        [SerializeField] private Transform rightDoor;
        [SerializeField] private MeshRenderer leftDoorRenderer;
        [SerializeField] private MeshRenderer rightDoorRenderer;
        
        [Header("Settings")]
        [SerializeField] private float doorOpenDistance = 3f;
        
        [Header("Health Colors")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color damagedColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private Color destroyedColor = new Color(0.3f, 0.3f, 0.3f); // Gray when destroyed
        
        [Header("Impact Flash")]
        [SerializeField] private Color impactFlashColor = Color.white;
        [SerializeField] private float flashDuration = 0.1f;

        private GateSystem _gateSystem;
        private Material _leftDoorMaterial;
        private Material _rightDoorMaterial;
        private Vector3 _leftDoorClosedPosition;
        private Vector3 _rightDoorClosedPosition;
        
        private float _flashTimer = 0f;
        private float _lastHealth = 100f;

        public void Initialize(GateSystem gateSystem)
        {
            _gateSystem = gateSystem;

            if (leftDoor != null)
            {
                _leftDoorClosedPosition = leftDoor.localPosition;
            }
            
            if (rightDoor != null)
            {
                _rightDoorClosedPosition = rightDoor.localPosition;
            }

            // Create materials
            if (leftDoorRenderer != null)
            {
                _leftDoorMaterial = leftDoorRenderer.material;
            }
            
            if (rightDoorRenderer != null)
            {
                _rightDoorMaterial = rightDoorRenderer.material;
            }

            // Subscribe to events
            _gateSystem.OnHealthChanged += OnHealthChanged;
            _gateSystem.OnOpenStateChanged += OnOpenStateChanged;
            
            UpdateHealthColor();
        }

        private void Update()
        {
            if (_gateSystem == null) return;

            // Update door positions based on gate state
            UpdateDoorPositions();
            
            // Check for damage impacts
            DetectDamageImpact();
            
            // Update flash effect
            UpdateFlashEffect();
        }

        private void DetectDamageImpact()
        {
            if (_gateSystem.Health < _lastHealth)
            {
                // Trigger flash
                _flashTimer = flashDuration;
            }
            _lastHealth = _gateSystem.Health;
        }

        private void UpdateFlashEffect()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                
                if (_flashTimer <= 0f)
                {
                    // Flash ended, restore normal color
                    UpdateHealthColor();
                }
                else
                {
                    // Flash active
                    if (_leftDoorMaterial != null)
                        _leftDoorMaterial.color = impactFlashColor;
                    
                    if (_rightDoorMaterial != null)
                        _rightDoorMaterial.color = impactFlashColor;
                }
            }
        }

        private void UpdateDoorPositions()
        {
            if (leftDoor == null || rightDoor == null) return;

            Vector3 leftTarget = _leftDoorClosedPosition;
            Vector3 rightTarget = _rightDoorClosedPosition;

            if (_gateSystem.IsOpen)
            {
                leftTarget += Vector3.left * doorOpenDistance;
                rightTarget += Vector3.right * doorOpenDistance;
            }

            leftDoor.localPosition = Vector3.Lerp(leftDoor.localPosition, leftTarget, Time.deltaTime * 3f);
            rightDoor.localPosition = Vector3.Lerp(rightDoor.localPosition, rightTarget, Time.deltaTime * 3f);
        }

        private void OnHealthChanged(float health)
        {
            UpdateHealthColor();
        }

        private void OnOpenStateChanged(bool isOpen)
        {
            // Visual feedback can be added here (e.g., particle effects)
            Debug.Log($"Gate is now {(isOpen ? "OPEN" : "CLOSED")}");
        }

        private void UpdateHealthColor()
        {
            if (_gateSystem == null) return;

            Color targetColor;
            float healthPercent = _gateSystem.HealthPercentage;

            if (healthPercent <= 0f)
                targetColor = destroyedColor; // Gray when destroyed
            else if (healthPercent > 0.6f)
                targetColor = healthyColor;
            else if (healthPercent > 0.3f)
                targetColor = damagedColor;
            else
                targetColor = criticalColor;

            if (_leftDoorMaterial != null)
                _leftDoorMaterial.color = targetColor;
            
            if (_rightDoorMaterial != null)
                _rightDoorMaterial.color = targetColor;
        }

        public void ResetState()
        {
            _flashTimer = 0f;
            _lastHealth = 100f;
            UpdateHealthColor();
        }

        private void OnDestroy()
        {
            if (_gateSystem != null)
            {
                _gateSystem.OnHealthChanged -= OnHealthChanged;
                _gateSystem.OnOpenStateChanged -= OnOpenStateChanged;
            }

            // Clean up materials
            if (_leftDoorMaterial != null)
                Destroy(_leftDoorMaterial);
            
            if (_rightDoorMaterial != null)
                Destroy(_rightDoorMaterial);
        }
    }
}

