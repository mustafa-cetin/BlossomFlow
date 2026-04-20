#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Reflex.Core;
using Reflex.Injectors;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlossomFlow.Application.Debugging
{
    public sealed class RuntimeDebugOverlayBootstrapper : IDisposable
    {
        private const string OverlayObjectName = "Blossom Flow Runtime Debug Overlay";

        private readonly Container _container;
        private GameObject _overlayObject;

        public RuntimeDebugOverlayBootstrapper(Container container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureOverlay();
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_overlayObject != null)
            {
                UnityEngine.Object.Destroy(_overlayObject);
                _overlayObject = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureOverlay();
        }

        private void EnsureOverlay()
        {
            if (_overlayObject != null)
            {
                return;
            }

            _overlayObject = new GameObject(OverlayObjectName);
            UnityEngine.Object.DontDestroyOnLoad(_overlayObject);
            _overlayObject.AddComponent<GameDebugOverlayView>();
            GameObjectInjector.InjectSingle(_overlayObject, _container);
        }
    }
}
#endif
