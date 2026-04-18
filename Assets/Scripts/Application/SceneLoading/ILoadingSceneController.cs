using System;
using System.Collections;

namespace BlossomFlow.Application.SceneLoading
{
    public interface ILoadingSceneController
    {
        IEnumerator LoadPendingTargetAsync(Action<float> reportProgress);
    }
}
