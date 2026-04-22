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
