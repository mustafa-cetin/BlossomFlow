using System;
using System.Collections;
using BlossomFlow.Application.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class UnityLoadingSceneController : ILoadingSceneController
    {
        private const float StartupServicesProgress = 0.15f;

        private readonly ILoadingSceneTargetProvider _targetProvider;
        private readonly IStartupServiceInitializer _startupServiceInitializer;
        private readonly IRemoteLevelCatalogPreloader _remoteLevelCatalogPreloader;

        public UnityLoadingSceneController(
            ILoadingSceneTargetProvider targetProvider,
            IStartupServiceInitializer startupServiceInitializer,
            IRemoteLevelCatalogPreloader remoteLevelCatalogPreloader)
        {
            _targetProvider = targetProvider;
            _startupServiceInitializer = startupServiceInitializer;
            _remoteLevelCatalogPreloader = remoteLevelCatalogPreloader;
        }

        public IEnumerator LoadPendingTargetAsync(Action<float> reportProgress)
        {
            reportProgress?.Invoke(0f);

            var startupServicesInitialized = false;
            if (!TryGetTarget(out var target, out var targetException))
            {
                if (targetException is InvalidOperationException)
                {
                    yield return InitializeStartupServices(reportProgress);
                    startupServicesInitialized = true;

                    if (!TryGetTarget(out target, out targetException))
                    {
                        LogTargetError(targetException);
                        yield break;
                    }
                }
                else
                {
                    LogTargetError(targetException);
                    yield break;
                }
            }

            var loadStartProgress = 0f;
            if (target.InitializeStartupServices && !startupServicesInitialized)
            {
                yield return InitializeStartupServices(reportProgress);
                yield return WaitForLevelCatalogReadiness();
                loadStartProgress = StartupServicesProgress;
            }
            else if (startupServicesInitialized)
            {
                yield return WaitForLevelCatalogReadiness();
                loadStartProgress = StartupServicesProgress;
            }

            var loadOperation = SceneManager.LoadSceneAsync(target.SceneName);
            if (loadOperation == null)
            {
                Debug.LogError($"Could not start async loading for scene '{target.SceneName}'.");
                _targetProvider.ClearPendingTargetScene();
                yield break;
            }

            loadOperation.allowSceneActivation = false;

            while (loadOperation.progress < 0.9f)
            {
                var loadProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);
                reportProgress?.Invoke(Mathf.Lerp(loadStartProgress, 1f, loadProgress));
                yield return null;
            }

            reportProgress?.Invoke(1f);
            _targetProvider.ClearPendingTargetScene();
            loadOperation.allowSceneActivation = true;

            while (!loadOperation.isDone)
            {
                yield return null;
            }
        }

        private IEnumerator InitializeStartupServices(Action<float> reportProgress)
        {
            if (_startupServiceInitializer != null)
            {
                yield return _startupServiceInitializer.InitializeIfNeeded();
            }

            reportProgress?.Invoke(StartupServicesProgress);
        }

        private IEnumerator WaitForLevelCatalogReadiness()
        {
            if (_remoteLevelCatalogPreloader == null)
            {
                yield break;
            }

            while (!_remoteLevelCatalogPreloader.IsLoaded)
            {
                if (_remoteLevelCatalogPreloader.IsUnavailable)
                {
                    Debug.LogError($"Loading paused because the level catalog is unavailable. {_remoteLevelCatalogPreloader.UnavailableReason}");
                    while (_remoteLevelCatalogPreloader.IsUnavailable)
                    {
                        yield return null;
                    }
                }

                if (!_remoteLevelCatalogPreloader.IsLoaded)
                {
                    yield return _remoteLevelCatalogPreloader.LoadIfNeeded();
                }

                yield return null;
            }
        }

        private bool TryGetTarget(out LoadingSceneTarget target, out Exception exception)
        {
            try
            {
                exception = null;
                return _targetProvider.TryGetTarget(out target);
            }
            catch (Exception caughtException)
            {
                target = default;
                exception = caughtException;
                return false;
            }
        }

        private static void LogTargetError(Exception exception)
        {
            if (exception != null)
            {
                Debug.LogError($"Loading scene could not resolve a target scene. {exception.Message}");
                return;
            }

            Debug.LogError("Loading scene opened without a pending target scene.");
        }
    }
}
