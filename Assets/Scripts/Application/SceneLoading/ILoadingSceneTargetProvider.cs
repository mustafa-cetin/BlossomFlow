namespace BlossomFlow.Application.SceneLoading
{
    public interface ILoadingSceneTargetProvider
    {
        bool TryGetPendingTargetScene(out string targetSceneName);
        void ClearPendingTargetScene();
    }
}
