using BlossomFlow.MainMenu.Domain;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MainMenuSwipeInput : MonoBehaviour
    {
        [SerializeField] private float minimumSwipeDistance = 80f;
        [SerializeField] private float maximumSwipeSeconds = 0.6f;

        private IMainMenuNavigator _navigator;
        private InputSystem_Actions _inputActions;
        private Vector2 _startPosition;
        private float _startTime;
        private bool _tracking;

        [Inject]
        private void Construct(IMainMenuNavigator navigator)
        {
            _navigator = navigator;
        }

        private void OnEnable()
        {
            EnsureInputActions();
            _inputActions.Player.Previous.performed += OnPreviousPerformed;
            _inputActions.Player.Next.performed += OnNextPerformed;
            _inputActions.UI.Click.started += OnPointerPressStarted;
            _inputActions.UI.Click.canceled += OnPointerPressCanceled;
            _inputActions.Player.Previous.Enable();
            _inputActions.Player.Next.Enable();
            _inputActions.UI.Point.Enable();
            _inputActions.UI.Click.Enable();
        }

        private void OnDisable()
        {
            if (_inputActions == null)
            {
                return;
            }

            _inputActions.Player.Previous.performed -= OnPreviousPerformed;
            _inputActions.Player.Next.performed -= OnNextPerformed;
            _inputActions.UI.Click.started -= OnPointerPressStarted;
            _inputActions.UI.Click.canceled -= OnPointerPressCanceled;
            _inputActions.Player.Previous.Disable();
            _inputActions.Player.Next.Disable();
            _inputActions.UI.Point.Disable();
            _inputActions.UI.Click.Disable();
            _tracking = false;
        }

        private void OnDestroy()
        {
            _inputActions?.Dispose();
            _inputActions = null;
        }

        private void EnsureInputActions()
        {
            if (_inputActions == null)
            {
                _inputActions = new InputSystem_Actions();
            }
        }

        private void OnPreviousPerformed(InputAction.CallbackContext context)
        {
            _navigator?.TryMove(-1);
        }

        private void OnNextPerformed(InputAction.CallbackContext context)
        {
            _navigator?.TryMove(1);
        }

        private void OnPointerPressStarted(InputAction.CallbackContext context)
        {
            Begin(_inputActions.UI.Point.ReadValue<Vector2>());
        }

        private void OnPointerPressCanceled(InputAction.CallbackContext context)
        {
            End(_inputActions.UI.Point.ReadValue<Vector2>());
        }

        private void Begin(Vector2 position)
        {
            _tracking = true;
            _startPosition = position;
            _startTime = Time.unscaledTime;
        }

        private void End(Vector2 position)
        {
            if (!_tracking)
            {
                return;
            }

            _tracking = false;
            var delta = position - _startPosition;
            if (Time.unscaledTime - _startTime > maximumSwipeSeconds)
            {
                return;
            }

            if (Mathf.Abs(delta.x) < minimumSwipeDistance || Mathf.Abs(delta.x) < Mathf.Abs(delta.y))
            {
                return;
            }

            _navigator?.TryMove(delta.x > 0f ? -1 : 1);
        }
    }
}
