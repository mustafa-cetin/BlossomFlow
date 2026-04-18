namespace BlossomFlow.Application.SceneLoading
{
    public readonly struct LoadingSceneTarget
    {
        public LoadingSceneTarget(string sceneName, bool initializeStartupServices)
        {
            SceneName = sceneName;
            InitializeStartupServices = initializeStartupServices;
        }

        public string SceneName { get; }
        public bool InitializeStartupServices { get; }
    }
}
