using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class UnityLoadingSceneController : ILoadingSceneController
    {
        private const float StartupServicesProgress = 0.15f;

        private readonly ILoadingSceneTargetProvider _targetProvider;
        private readonly IStartupServiceInitializer _startupServiceInitializer;

        public UnityLoadingSceneController(
            ILoadingSceneTargetProvider targetProvider,
            IStartupServiceInitializer startupServiceInitializer)
        {
            _targetProvider = targetProvider;
            _startupServiceInitializer = startupServiceInitializer;
        }

        public IEnumerator LoadPendingTargetAsync(Action<float> reportProgress)
        {
            reportProgress?.Invoke(0f);

            if (!_targetProvider.TryGetTarget(out var target))
            {
                Debug.LogError("Loading scene opened without a pending target scene.");
                yield break;
            }

            if (target.InitializeStartupServices)
            {
                yield return _startupServiceInitializer.InitializeIfNeeded();
                reportProgress?.Invoke(StartupServicesProgress);
            }

            var loadStartProgress = target.InitializeStartupServices ? StartupServicesProgress : 0f;
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
    }
}
