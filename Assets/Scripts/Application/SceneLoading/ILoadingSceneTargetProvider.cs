namespace BlossomFlow.Application.SceneLoading
{
    public interface ILoadingSceneTargetProvider
    {
        bool TryGetTarget(out LoadingSceneTarget target);
        void ClearPendingTargetScene();
    }
}
