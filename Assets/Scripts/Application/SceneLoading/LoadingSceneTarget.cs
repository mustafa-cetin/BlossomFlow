namespace BlossomFlow.Application.SceneLoading
{
    public readonly struct LoadingSceneTarget
    {
        public LoadingSceneTarget(string sceneAddress, bool initializeStartupServices)
        {
            SceneAddress = sceneAddress;
            InitializeStartupServices = initializeStartupServices;
        }

        public string SceneAddress { get; }
        public bool InitializeStartupServices { get; }
    }
}
