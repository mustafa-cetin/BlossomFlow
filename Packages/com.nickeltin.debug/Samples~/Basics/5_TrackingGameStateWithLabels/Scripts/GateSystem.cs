using System;
using UnityEngine;

namespace nickeltin.DebugPanel.Examples.Runtime
{
    public enum GameState
    {
        Idle,       // No enemies, gate closed
        Active,     // Enemies present, defending
        Defeated,   // Gate destroyed or breached
        Victory     // All enemies cleared, gate intact
    }

    /// <summary>
    /// Core gate system logic - manages state, health, and enemy tracking
    /// </summary>
    public class GateSystem
    {
        public event Action OnStateChanged;
        public event Action<float> OnHealthChanged;
        public event Action<bool> OnOpenStateChanged;
        public event Action<int> OnEnemyCountChanged;
        public event Action<GameState> OnGameStateChanged;

        public float MaxHealth { get; private set; } = 100f;
        public float Health { get; private set; } = 100f;
        public bool IsOpen { get; private set; } = false;
        public int EnemyCount { get; private set; } = 0;
        public float TotalDamageReceived { get; private set; } = 0f;
        public float Position { get; private set; } = 0f;
        public float HealthPercentage => Health / MaxHealth;
        public GameState CurrentGameState { get; private set; } = GameState.Idle;

        // Enemy attack settings
        public float DamagePerEnemyPerSecond { get; set; } = 2f;
        public bool EnemiesCanAttack { get; set; } = true;

        private float _targetPosition = 0f;
        private const float OpenPosition = 3f;
        private const float ClosedPosition = 0f;
        private const float PositionLerpSpeed = 3f;

        public GateSystem()
        {
            // Spawn one enemy at start
            AddEnemy();
        }

        public void Update(float deltaTime)
        {
            // Smoothly move position towards target
            if (Mathf.Abs(Position - _targetPosition) > 0.01f)
            {
                Position = Mathf.Lerp(Position, _targetPosition, deltaTime * PositionLerpSpeed);
            }

            // Enemies attack the gate
            if (EnemiesCanAttack && EnemyCount > 0 && Health > 0)
            {
                float damageThisFrame = EnemyCount * DamagePerEnemyPerSecond * deltaTime;
                TakeDamage(damageThisFrame);
            }

            // Update game state
            UpdateGameState();
        }

        private void UpdateGameState()
        {
            GameState newState = CurrentGameState;

            if (CurrentGameState == GameState.Defeated)
            {
                // Stay defeated
                return;
            }

            if (EnemyCount == 0 && Health > 0 && !IsOpen)
            {
                newState = GameState.Idle;
            }
            else if (EnemyCount > 0 && Health > 0)
            {
                newState = GameState.Active;
            }

            if (newState != CurrentGameState)
            {
                CurrentGameState = newState;
                OnGameStateChanged?.Invoke(CurrentGameState);
                OnStateChanged?.Invoke();
            }
        }

        public void Open()
        {
            if (!IsOpen && CurrentGameState != GameState.Defeated)
            {
                IsOpen = true;
                _targetPosition = OpenPosition;
                OnOpenStateChanged?.Invoke(IsOpen);
                OnStateChanged?.Invoke();
                
                // Check for defeat - opening with enemies present
                if (EnemyCount > 0)
                {
                    SetGameState(GameState.Defeated);
                    Debug.Log("<color=red>DEFEAT! Gate opened with enemies present!</color>");
                }
            }
        }

        public void Close()
        {
            // Can't close if destroyed or defeated
            if (IsOpen && Health > 0 && CurrentGameState != GameState.Defeated)
            {
                IsOpen = false;
                _targetPosition = ClosedPosition;
                OnOpenStateChanged?.Invoke(IsOpen);
                OnStateChanged?.Invoke();
            }
        }

        public void TakeDamage(float damage)
        {
            if (Health <= 0 || CurrentGameState == GameState.Defeated) return; // Already destroyed or defeated
            
            float oldHealth = Health;
            Health = Mathf.Max(0, Health - damage);
            TotalDamageReceived += damage;
            
            // Door breaks open when destroyed
            if (Health <= 0)
            {
                if (!IsOpen)
                {
                    IsOpen = true;
                    _targetPosition = OpenPosition;
                    OnOpenStateChanged?.Invoke(IsOpen);
                }
                SetGameState(GameState.Defeated);
                Debug.Log("<color=red>DEFEAT! Gate destroyed!</color>");
            }

            if (!Mathf.Approximately(oldHealth, Health))
            {
                OnHealthChanged?.Invoke(Health);
            }
            
            OnStateChanged?.Invoke();
        }

        public void Repair(float amount)
        {
            float oldHealth = Health;
            Health = Mathf.Min(MaxHealth, Health + amount);
            
            if (!Mathf.Approximately(oldHealth, Health))
            {
                OnHealthChanged?.Invoke(Health);
            }
            
            OnStateChanged?.Invoke();
        }
    

        public void AddEnemy()
        {
            EnemyCount++;
            OnEnemyCountChanged?.Invoke(EnemyCount);
            OnStateChanged?.Invoke();
            
            if (EnemyCount == 1)
            {
                Debug.Log($"<color=red>Enemies are attacking! ({DamagePerEnemyPerSecond} damage/enemy/second)</color>");
            }
        }

        public void RemoveEnemy()
        {
            int oldCount = EnemyCount;
            EnemyCount = Mathf.Max(0, EnemyCount - 1);
            
            if (oldCount != EnemyCount)
            {
                OnEnemyCountChanged?.Invoke(EnemyCount);
                OnStateChanged?.Invoke();
                
                if (EnemyCount == 0)
                {
                    Debug.Log("<color=green>All enemies cleared!</color>");
                }
            }
        }

        public void SetEnemyDamageRate(float damagePerSecond)
        {
            DamagePerEnemyPerSecond = Mathf.Max(0, damagePerSecond);
            Debug.Log($"Enemy damage rate set to {DamagePerEnemyPerSecond}/second");
        }

        private void SetGameState(GameState newState)
        {
            if (CurrentGameState != newState)
            {
                CurrentGameState = newState;
                OnGameStateChanged?.Invoke(CurrentGameState);
                OnStateChanged?.Invoke();
            }
        }

        public void Reset()
        {
            Health = MaxHealth;
            IsOpen = false;
            EnemyCount = 0;
            TotalDamageReceived = 0f;
            Position = 0f;
            _targetPosition = ClosedPosition;
            EnemiesCanAttack = true;
            DamagePerEnemyPerSecond = 2f;
            CurrentGameState = GameState.Idle;
            
            OnHealthChanged?.Invoke(Health);
            OnOpenStateChanged?.Invoke(IsOpen);
            OnEnemyCountChanged?.Invoke(EnemyCount);
            OnGameStateChanged?.Invoke(CurrentGameState);
            OnStateChanged?.Invoke();
        }
    }
}

