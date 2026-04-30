using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace BlossomFlow.Application.SceneLoading
{
    public interface ILoadingSceneController
    {
        UniTask LoadPendingTargetAsync(Action<float> reportProgress, CancellationToken cancellationToken);
    }
}
