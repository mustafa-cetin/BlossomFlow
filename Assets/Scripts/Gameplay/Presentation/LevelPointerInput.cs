using System;
using BlossomFlow.Gameplay.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlossomFlow.Gameplay.Presentation
{
    public sealed class LevelPointerInput : MonoBehaviour
    {
        private Camera _camera;
        private InputSystem_Actions _inputActions;
        private Vector2 _pendingScreenPosition;
        private bool _hasPendingClick;
        private bool _isPointerInputArmed;
        private int _pointerInputArmedFrame = -1;

        private void OnEnable()
        {
            EnsureInputActions();
            _inputActions.UI.Click.performed += OnClickPerformed;
            _inputActions.UI.Point.Enable();
            _inputActions.UI.Click.Enable();
            ResetPointerState();
        }

        private void OnDisable()
        {
            if (_inputActions == null)
            {
                return;
            }

            _inputActions.UI.Click.performed -= OnClickPerformed;
            _inputActions.UI.Point.Disable();
            _inputActions.UI.Click.Disable();
            ResetPointerState();
        }

        private void OnDestroy()
        {
            _inputActions?.Dispose();
            _inputActions = null;
        }

        public void Tick(LevelSession session, Action<LevelClickTarget> clicked)
        {
            if (session == null || session.State != LevelPlayState.Playing)
            {
                _hasPendingClick = false;
                return;
            }

            RefreshPointerArmState();
            if (!_isPointerInputArmed)
            {
                _hasPendingClick = false;
                return;
            }

            if (_hasPendingClick)
            {
                _hasPendingClick = false;
                TryClickScreenPosition(_pendingScreenPosition, clicked);
            }
        }

        private void EnsureInputActions()
        {
            if (_inputActions == null)
            {
                _inputActions = new InputSystem_Actions();
            }
        }

        private void OnClickPerformed(InputAction.CallbackContext context)
        {
            if (!_isPointerInputArmed || Time.frameCount <= _pointerInputArmedFrame)
            {
                return;
            }

            _pendingScreenPosition = _inputActions.UI.Point.ReadValue<Vector2>();
            _hasPendingClick = true;
        }

        public void RequirePointerReleaseBeforeNextClick()
        {
            ResetPointerState();
        }

        private void ResetPointerState()
        {
            _hasPendingClick = false;
            _isPointerInputArmed = false;
            _pointerInputArmedFrame = -1;
        }

        private void RefreshPointerArmState()
        {
            if (_isPointerInputArmed)
            {
                return;
            }

            if (IsAnyPointerPressed())
            {
                return;
            }

            _isPointerInputArmed = true;
            _pointerInputArmedFrame = Time.frameCount;
        }

        private static bool IsAnyPointerPressed()
        {
            if (Touchscreen.current != null)
            {
                var touches = Touchscreen.current.touches;
                for (var i = 0; i < touches.Count; i++)
                {
                    if (touches[i].press.isPressed)
                    {
                        return true;
                    }
                }
            }

            return Mouse.current != null && Mouse.current.leftButton.isPressed;
        }

        private void TryClickScreenPosition(Vector2 screenPosition, Action<LevelClickTarget> clicked)
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                Debug.LogWarning("LevelPointerInput could not find a MainCamera for 2D clicks.");
                return;
            }

            var worldPosition = _camera.ScreenToWorldPoint(screenPosition);
            var hits = Physics2D.OverlapPointAll(worldPosition);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            LevelClickTarget bestTarget = null;
            var bestSortingLayer = int.MinValue;
            var bestSortingOrder = int.MinValue;
            var bestDepth = float.NegativeInfinity;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null || !hit.enabled)
                {
                    continue;
                }

                var target = hit.GetComponent<LevelClickTarget>();
                if (target == null)
                {
                    continue;
                }

                var sortingLayer = GetSortingLayerValue(hit);
                var sortingOrder = GetSortingOrder(hit);
                var depth = hit.transform.position.z;
                var distance = ((Vector2)hit.bounds.center - (Vector2)worldPosition).sqrMagnitude;
                if (bestTarget == null ||
                    sortingLayer > bestSortingLayer ||
                    (sortingLayer == bestSortingLayer && sortingOrder > bestSortingOrder) ||
                    (sortingLayer == bestSortingLayer && sortingOrder == bestSortingOrder && depth > bestDepth) ||
                    (sortingLayer == bestSortingLayer && sortingOrder == bestSortingOrder && Mathf.Approximately(depth, bestDepth) && distance < bestDistance))
                {
                    bestSortingLayer = sortingLayer;
                    bestSortingOrder = sortingOrder;
                    bestDepth = depth;
                    bestDistance = distance;
                    bestTarget = target;
                }
            }

            if (bestTarget != null)
            {
                clicked?.Invoke(bestTarget);
            }
        }

        private static int GetSortingLayerValue(Collider2D hit)
        {
            var renderer = hit != null ? hit.GetComponentInParent<Renderer>() : null;
            return renderer != null ? SortingLayer.GetLayerValueFromID(renderer.sortingLayerID) : 0;
        }

        private static int GetSortingOrder(Collider2D hit)
        {
            var renderer = hit != null ? hit.GetComponentInParent<Renderer>() : null;
            return renderer != null ? renderer.sortingOrder : 0;
        }
    }
}
