using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class UnityLoadingSceneController : ILoadingSceneController
    {
        private readonly ILoadingSceneTargetProvider _targetProvider;

        public UnityLoadingSceneController(ILoadingSceneTargetProvider targetProvider)
        {
            _targetProvider = targetProvider;
        }

        public IEnumerator LoadPendingTargetAsync(Action<float> reportProgress)
        {
            reportProgress?.Invoke(0f);

            if (!_targetProvider.TryGetPendingTargetScene(out var targetSceneName))
            {
                Debug.LogError("Loading scene opened without a pending target scene.");
                yield break;
            }

            var loadOperation = SceneManager.LoadSceneAsync(targetSceneName);
            if (loadOperation == null)
            {
                Debug.LogError($"Could not start async loading for scene '{targetSceneName}'.");
                _targetProvider.ClearPendingTargetScene();
                yield break;
            }

            loadOperation.allowSceneActivation = false;

            while (loadOperation.progress < 0.9f)
            {
                reportProgress?.Invoke(Mathf.Clamp01(loadOperation.progress / 0.9f));
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
