using System.Collections.Generic;
using UnityEngine;

namespace nickeltin.DebugPanel.Examples.Runtime
{
    /// <summary>
    /// Visualizes enemies as simple spheres orbiting the gate
    /// </summary>
    public class EnemyVisualizer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float orbitRadius = 8f;
        [SerializeField] private float orbitSpeed = 30f;
        [SerializeField] private float enemySize = 0.8f;
        [SerializeField] private float passThroughSpeed = 5f;
        
        [Header("Visual Feedback")]
        [SerializeField] private Color enemyColor = new Color(0.9f, 0.2f, 0.2f); // Red

        private GateSystem _gateSystem;
        private List<GameObject> _enemyObjects = new List<GameObject>();
        private Material _enemyMaterial;

        public void Initialize(GateSystem gateSystem)
        {
            _gateSystem = gateSystem;
            _gateSystem.OnEnemyCountChanged += OnEnemyCountChanged;
            
            // Create shared material
            _enemyMaterial = new Material(Shader.Find("Standard"));
            _enemyMaterial.color = enemyColor;
            _enemyMaterial.SetFloat("_Metallic", 0.2f);
            _enemyMaterial.SetFloat("_Glossiness", 0.8f);
            
            // Initial sync
            SyncEnemyCount(_gateSystem.EnemyCount);
        }

        private void Update()
        {
            // Rotate enemies around the gate
            RotateEnemies();
        }

        private void OnEnemyCountChanged(int newCount)
        {
            SyncEnemyCount(newCount);
        }

        private void SyncEnemyCount(int targetCount)
        {
            // Add enemies if needed
            while (_enemyObjects.Count < targetCount)
            {
                CreateEnemy(_enemyObjects.Count);
            }

            // Remove enemies if needed
            while (_enemyObjects.Count > targetCount)
            {
                RemoveEnemy();
            }

            // Reposition all enemies evenly
            RepositionEnemies();
        }

        private void CreateEnemy(int index)
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            enemy.name = $"Enemy_{index}";
            enemy.transform.SetParent(transform);
            enemy.transform.localScale = Vector3.one * enemySize;
            
            // Apply material
            if (_enemyMaterial != null)
            {
                enemy.GetComponent<MeshRenderer>().material = _enemyMaterial;
            }
            
            // Remove collider
            var collider = enemy.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
            
            _enemyObjects.Add(enemy);
        }

        private void RemoveEnemy()
        {
            if (_enemyObjects.Count == 0) return;

            GameObject lastEnemy = _enemyObjects[_enemyObjects.Count - 1];
            _enemyObjects.RemoveAt(_enemyObjects.Count - 1);
            Destroy(lastEnemy);
        }

        private void RepositionEnemies()
        {
            for (int i = 0; i < _enemyObjects.Count; i++)
            {
                float angle = (360f / _enemyObjects.Count) * i;
                _enemyObjects[i].transform.rotation = Quaternion.Euler(0, angle, 0);
            }
        }

        private void RotateEnemies()
        {
            // Enemies rush through when gate is open (either destroyed or manually opened)
            bool gateOpen = _gateSystem != null && _gateSystem.IsOpen;
            
            for (int i = _enemyObjects.Count - 1; i >= 0; i--)
            {
                var enemy = _enemyObjects[i];
                if (enemy == null) continue;
                
                if (gateOpen)
                {
                    // Rush through the open gate
                    enemy.transform.position += Vector3.forward * passThroughSpeed * Time.deltaTime;
                    
                    // Remove if passed through
                    if (enemy.transform.position.z > transform.position.z + 10f)
                    {
                        Destroy(enemy);
                        _enemyObjects.RemoveAt(i);
                        
                        // Update gate system enemy count
                        if (_gateSystem != null && _gateSystem.EnemyCount > 0)
                        {
                            _gateSystem.RemoveEnemy();
                        }
                    }
                }
                else
                {
                    // Orbit around the gate
                    enemy.transform.RotateAround(transform.position, Vector3.up, orbitSpeed * Time.deltaTime);
                    
                    // Keep distance
                    Vector3 direction = (enemy.transform.position - transform.position).normalized;
                    enemy.transform.position = transform.position + direction * orbitRadius;
                    
                    // Bob up and down
                    float bobOffset = Mathf.Sin(Time.time * 2f + enemy.GetInstanceID()) * 0.3f;
                    enemy.transform.localPosition = new Vector3(
                        enemy.transform.localPosition.x,
                        2.5f + bobOffset,
                        enemy.transform.localPosition.z
                    );
                }
            }
        }

        public void ResetState()
        {
            // Clear all enemy visuals
            foreach (var enemy in _enemyObjects)
            {
                if (enemy != null)
                    Destroy(enemy);
            }
            _enemyObjects.Clear();
        }

        private void OnDestroy()
        {
            if (_gateSystem != null)
            {
                _gateSystem.OnEnemyCountChanged -= OnEnemyCountChanged;
            }

            // Clean up enemies
            foreach (var enemy in _enemyObjects)
            {
                if (enemy != null)
                    Destroy(enemy);
            }
            _enemyObjects.Clear();

            // Clean up material
            if (_enemyMaterial != null)
                Destroy(_enemyMaterial);
        }
    }
}

