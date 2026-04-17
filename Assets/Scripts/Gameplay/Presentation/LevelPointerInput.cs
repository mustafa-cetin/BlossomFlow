using System;
using BlossomFlow.Gameplay.Domain;
using UnityEngine;

namespace BlossomFlow.Gameplay.Presentation
{
    public sealed class LevelPointerInput : MonoBehaviour
    {
        private Camera _camera;

        public void Tick(LevelSession session, Action<LevelClickTarget> clicked)
        {
            if (session == null || session.State != LevelPlayState.Playing)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryClickScreenPosition(Input.mousePosition, clicked);
            }

            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    TryClickScreenPosition(touch.position, clicked);
                }
            }
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
            var hit = Physics2D.OverlapPoint(worldPosition);
            if (hit == null)
            {
                return;
            }

            var target = hit.GetComponent<LevelClickTarget>();
            if (target != null)
            {
                clicked?.Invoke(target);
            }
        }
    }
}
