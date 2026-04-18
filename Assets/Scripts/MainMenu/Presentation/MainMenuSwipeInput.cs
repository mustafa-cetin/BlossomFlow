using BlossomFlow.MainMenu.Domain;
using Reflex.Attributes;
using UnityEngine;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MainMenuSwipeInput : MonoBehaviour
    {
        [SerializeField] private float minimumSwipeDistance = 80f;
        [SerializeField] private float maximumSwipeSeconds = 0.6f;

        private IMainMenuNavigator _navigator;
        private Vector2 _startPosition;
        private float _startTime;
        private bool _tracking;

        [Inject]
        private void Construct(IMainMenuNavigator navigator)
        {
            _navigator = navigator;
        }

        private void Update()
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                TrackTouch(touch.phase, touch.position);
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Begin(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                End(Input.mousePosition);
            }
        }

        private void TrackTouch(TouchPhase phase, Vector2 position)
        {
            if (phase == TouchPhase.Began)
            {
                Begin(position);
            }
            else if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
            {
                End(position);
            }
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
