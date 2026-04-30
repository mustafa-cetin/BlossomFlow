using System;
using System.Threading;
using BlossomFlow.Application.Levels;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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

        public async UniTask LoadPendingTargetAsync(Action<float> reportProgress, CancellationToken cancellationToken)
        {
            reportProgress?.Invoke(0f);

            var startupServicesInitialized = false;
            if (!TryGetTarget(out var target, out var targetException))
            {
                if (targetException is InvalidOperationException)
                {
                    await InitializeStartupServices(reportProgress, cancellationToken);
                    startupServicesInitialized = true;

                    if (!TryGetTarget(out target, out targetException))
                    {
                        LogTargetError(targetException);
                        return;
                    }
                }
                else
                {
                    LogTargetError(targetException);
                    return;
                }
            }

            var loadStartProgress = 0f;
            if (target.InitializeStartupServices && !startupServicesInitialized)
            {
                await InitializeStartupServices(reportProgress, cancellationToken);
                await WaitForLevelCatalogReadiness(cancellationToken);
                loadStartProgress = StartupServicesProgress;
            }
            else if (startupServicesInitialized)
            {
                await WaitForLevelCatalogReadiness(cancellationToken);
                loadStartProgress = StartupServicesProgress;
            }

            await LoadAddressableScene(target.SceneAddress, loadStartProgress, reportProgress, cancellationToken);
        }

        private async UniTask LoadAddressableScene(
            string sceneAddress,
            float loadStartProgress,
            Action<float> reportProgress,
            CancellationToken cancellationToken)
        {
            var loadOperation = Addressables.LoadSceneAsync(sceneAddress, LoadSceneMode.Single, false);

            while (!loadOperation.IsDone)
            {
                var loadProgress = Mathf.Clamp01(loadOperation.PercentComplete);
                reportProgress?.Invoke(Mathf.Lerp(loadStartProgress, 1f, loadProgress));
                await UniTask.Yield(cancellationToken);
            }

            if (loadOperation.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogError($"Could not load addressable scene '{sceneAddress}'. {loadOperation.OperationException?.Message}");
                _targetProvider.ClearPendingTargetScene();
                return;
            }

            reportProgress?.Invoke(1f);
            _targetProvider.ClearPendingTargetScene();

            await loadOperation.Result.ActivateAsync().ToUniTask(cancellationToken: cancellationToken);
        }

        private async UniTask InitializeStartupServices(Action<float> reportProgress, CancellationToken cancellationToken)
        {
            if (_startupServiceInitializer != null)
            {
                await _startupServiceInitializer.InitializeIfNeeded().ToUniTask(cancellationToken: cancellationToken);
            }

            reportProgress?.Invoke(StartupServicesProgress);
        }

        private async UniTask WaitForLevelCatalogReadiness(CancellationToken cancellationToken)
        {
            if (_remoteLevelCatalogPreloader == null)
            {
                return;
            }

            while (!_remoteLevelCatalogPreloader.IsLoaded)
            {
                if (_remoteLevelCatalogPreloader.IsUnavailable)
                {
                    Debug.LogError($"Loading paused because the level catalog is unavailable. {_remoteLevelCatalogPreloader.UnavailableReason}");
                    while (_remoteLevelCatalogPreloader.IsUnavailable)
                    {
                        await UniTask.Yield(cancellationToken);
                    }
                }

                if (!_remoteLevelCatalogPreloader.IsLoaded)
                {
                    await _remoteLevelCatalogPreloader.LoadIfNeeded().ToUniTask(cancellationToken: cancellationToken);
                }

                await UniTask.Yield(cancellationToken);
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
