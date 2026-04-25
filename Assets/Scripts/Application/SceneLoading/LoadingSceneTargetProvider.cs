using BlossomFlow.Application.Tutorial;

namespace BlossomFlow.Application.SceneLoading
{
    public sealed class LoadingSceneTargetProvider : ILoadingSceneTargetProvider
    {
        private readonly string _startupTargetSceneName;
        private readonly string _startupTutorialCompletedTargetSceneName;
        private readonly IFirstLevelTutorialProgressService _firstLevelTutorialProgressService;
        private bool _startupTargetConsumed;

        public LoadingSceneTargetProvider(
            string startupTargetSceneName,
            string startupTutorialCompletedTargetSceneName,
            IFirstLevelTutorialProgressService firstLevelTutorialProgressService)
        {
            _startupTargetSceneName = startupTargetSceneName;
            _startupTutorialCompletedTargetSceneName = startupTutorialCompletedTargetSceneName;
            _firstLevelTutorialProgressService = firstLevelTutorialProgressService;
        }

        public bool TryGetTarget(out LoadingSceneTarget target)
        {
            var startupTargetSceneName = GetStartupTargetSceneName();
            if (!_startupTargetConsumed && !string.IsNullOrWhiteSpace(startupTargetSceneName))
            {
                _startupTargetConsumed = true;
                target = new LoadingSceneTarget(startupTargetSceneName.Trim(), true);
                return true;
            }

            target = default;
            return false;
        }

        public void ClearPendingTargetScene()
        {
        }

        private string GetStartupTargetSceneName()
        {
            return _firstLevelTutorialProgressService != null &&
                _firstLevelTutorialProgressService.IsCompleted &&
                !string.IsNullOrWhiteSpace(_startupTutorialCompletedTargetSceneName)
                    ? _startupTutorialCompletedTargetSceneName
                    : _startupTargetSceneName;
        }
    }
}
