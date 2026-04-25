#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlossomFlow.Application.Debugging
{
    public sealed class GameDebugOverlayView : MonoBehaviour
    {
        private const int WindowId = 74623;
        private const float WindowWidth = 340f;
        private const float WindowHeight = 430f;

        private readonly int[] _quickGoldAmounts = { -100, -10, 10, 100, 1000 };
        private readonly int[] _quickSeedAmounts = { -10, -1, 1, 10, 100 };

        private IGameDebugStateService _debugState;
        private InputAction _toggleOverlayAction;
        private Rect _windowRect = new Rect(20f, 20f, WindowWidth, WindowHeight);
        private bool _isVisible;
        private string _levelInput = "1";
        private string _goldInput = "0";
        private string _magicalSeedInput = "0";
        private string _currentLivesInput = "5";
        private string _maxLivesInput = "5";
        private string _stateReadinessMessage;

        [Inject]
        private void Construct(IGameDebugStateService debugState)
        {
            _debugState = debugState;
        }

        private void Awake()
        {
            _toggleOverlayAction = new InputAction("ToggleDebugOverlay", InputActionType.Button);
            _toggleOverlayAction.AddBinding("<Keyboard>/f10");
            _toggleOverlayAction.AddBinding("<Keyboard>/backquote");
        }

        private void OnEnable()
        {
            _toggleOverlayAction.performed += OnToggleOverlayPerformed;
            _toggleOverlayAction.Enable();
        }

        private void OnDisable()
        {
            if (_toggleOverlayAction == null)
            {
                return;
            }

            _toggleOverlayAction.performed -= OnToggleOverlayPerformed;
            _toggleOverlayAction.Disable();
        }

        private void OnDestroy()
        {
            _toggleOverlayAction?.Dispose();
            _toggleOverlayAction = null;
        }

        private void OnToggleOverlayPerformed(InputAction.CallbackContext context)
        {
            _isVisible = !_isVisible;
            if (_isVisible)
            {
                SyncInputsFromState();
            }
        }

        private void OnGUI()
        {
            if (!_isVisible || _debugState == null)
            {
                return;
            }

            _windowRect = GUI.Window(WindowId, _windowRect, DrawWindow, "Blossom Debug");
        }

        private void DrawWindow(int windowId)
        {
            if (!TryGetSnapshot(out var snapshot))
            {
                GUILayout.Label("Debug state is not ready yet.");
                if (!string.IsNullOrEmpty(_stateReadinessMessage))
                {
                    GUILayout.Label(_stateReadinessMessage);
                }

                GUILayout.Space(8f);
                GUILayout.Label("Try again after startup services finish.");
                GUILayout.Label("Toggle: F10 or `");
                GUI.DragWindow();
                return;
            }

            GUILayout.Label($"Level: {snapshot.LevelNumber} | Selected: {snapshot.SelectedLevelNumber} | Catalog: {GetLevelCountText(snapshot)}");
            GUILayout.Label($"Gold: {snapshot.Gold} | Magical Seed: {snapshot.MagicalSeed}");
            GUILayout.Label($"Lives: {snapshot.CurrentLives}/{snapshot.MaxLives} {snapshot.NextLifeCountdown}");

            GUILayout.Space(8f);
            DrawLevelControls();
            GUILayout.Space(8f);
            DrawGoldControls();
            GUILayout.Space(8f);
            DrawMagicalSeedControls();
            GUILayout.Space(8f);
            DrawLifeControls();
            GUILayout.Space(8f);
            DrawUtilityControls();

            GUI.DragWindow();
        }

        private void DrawLevelControls()
        {
            GUILayout.Label("Level");
            GUILayout.BeginHorizontal();
            _levelInput = GUILayout.TextField(_levelInput, GUILayout.Width(80f));
            if (GUILayout.Button("Set"))
            {
                SetLevel(false);
            }

            if (GUILayout.Button("Set + Reload"))
            {
                SetLevel(true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-1"))
            {
                ChangeLevel(-1, true);
            }

            if (GUILayout.Button("+1"))
            {
                ChangeLevel(1, true);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawGoldControls()
        {
            GUILayout.Label("Gold");
            GUILayout.BeginHorizontal();
            _goldInput = GUILayout.TextField(_goldInput, GUILayout.Width(80f));
            if (GUILayout.Button("Set"))
            {
                SetGoldFromInput();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            for (var i = 0; i < _quickGoldAmounts.Length; i++)
            {
                var amount = _quickGoldAmounts[i];
                if (GUILayout.Button(FormatSigned(amount)))
                {
                    ExecuteAndSync(() => _debugState.AddGold(amount));
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawMagicalSeedControls()
        {
            GUILayout.Label("Magical Seed");
            GUILayout.BeginHorizontal();
            _magicalSeedInput = GUILayout.TextField(_magicalSeedInput, GUILayout.Width(80f));
            if (GUILayout.Button("Set"))
            {
                SetMagicalSeedFromInput();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            for (var i = 0; i < _quickSeedAmounts.Length; i++)
            {
                var amount = _quickSeedAmounts[i];
                if (GUILayout.Button(FormatSigned(amount)))
                {
                    ExecuteAndSync(() => _debugState.AddMagicalSeed(amount));
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawLifeControls()
        {
            GUILayout.Label("Lives");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Current", GUILayout.Width(56f));
            _currentLivesInput = GUILayout.TextField(_currentLivesInput, GUILayout.Width(54f));
            GUILayout.Label("Max", GUILayout.Width(30f));
            _maxLivesInput = GUILayout.TextField(_maxLivesInput, GUILayout.Width(54f));
            if (GUILayout.Button("Set"))
            {
                SetLivesFromInput();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Refill Lives"))
            {
                ExecuteAndSync(() => _debugState.RefillLives());
            }
        }

        private void DrawUtilityControls()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sync Inputs"))
            {
                SyncInputsFromState();
            }

            if (GUILayout.Button("Reset Progress"))
            {
                ExecuteAndSync(() => _debugState.ResetProgress());
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Toggle: F10 or `");
        }

        private void ChangeLevel(int delta, bool reloadCurrentLevel)
        {
            if (!TryGetSnapshot(out var snapshot))
            {
                return;
            }

            var nextLevel = snapshot.LevelNumber + delta;
            ExecuteAndSync(() => _debugState.SetLevel(nextLevel, reloadCurrentLevel));
        }

        private void SetLevel(bool reloadCurrentLevel)
        {
            if (!int.TryParse(_levelInput, out var levelNumber))
            {
                SyncInputsFromState();
                return;
            }

            ExecuteAndSync(() => _debugState.SetLevel(levelNumber, reloadCurrentLevel));
        }

        private void SetGoldFromInput()
        {
            if (int.TryParse(_goldInput, out var amount))
            {
                ExecuteAndSync(() => _debugState.SetGold(amount));
                return;
            }

            SyncInputsFromState();
        }

        private void SetMagicalSeedFromInput()
        {
            if (int.TryParse(_magicalSeedInput, out var amount))
            {
                ExecuteAndSync(() => _debugState.SetMagicalSeed(amount));
                return;
            }

            SyncInputsFromState();
        }

        private void SetLivesFromInput()
        {
            if (int.TryParse(_currentLivesInput, out var currentLives) &&
                int.TryParse(_maxLivesInput, out var maxLives))
            {
                ExecuteAndSync(() => _debugState.SetLives(currentLives, maxLives));
                return;
            }

            SyncInputsFromState();
        }

        private void SyncInputsFromState()
        {
            if (!TryGetSnapshot(out var snapshot))
            {
                return;
            }

            _levelInput = snapshot.LevelNumber.ToString();
            _goldInput = snapshot.Gold.ToString();
            _magicalSeedInput = snapshot.MagicalSeed.ToString();
            _currentLivesInput = snapshot.CurrentLives.ToString();
            _maxLivesInput = snapshot.MaxLives.ToString();
        }

        private void ExecuteAndSync(Action action)
        {
            try
            {
                action?.Invoke();
                _stateReadinessMessage = null;
            }
            catch (InvalidOperationException exception)
            {
                _stateReadinessMessage = exception.Message;
            }

            SyncInputsFromState();
        }

        private bool TryGetSnapshot(out GameDebugSnapshot snapshot)
        {
            snapshot = default;

            if (_debugState == null)
            {
                _stateReadinessMessage = "Debug state service is not injected.";
                return false;
            }

            try
            {
                snapshot = _debugState.Current;
                _stateReadinessMessage = null;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                _stateReadinessMessage = exception.Message;
                return false;
            }
        }

        private static string GetLevelCountText(GameDebugSnapshot snapshot)
        {
            return snapshot.LevelCount > 0 ? snapshot.LevelCount.ToString() : "n/a";
        }

        private static string FormatSigned(int amount)
        {
            return amount > 0 ? $"+{amount}" : amount.ToString();
        }
    }
}
#endif
