namespace BlossomFlow.Application.Tutorial
{
    public sealed class FirstLevelTutorialProgressService : IFirstLevelTutorialProgressService
    {
        private const string CompletedKey = "blossom_flow_first_level_tutorial_completed";

        public bool IsCompleted => SafePlayerPrefs.GetInt(CompletedKey, 0) == 1;

        public void MarkCompleted()
        {
            if (IsCompleted)
            {
                return;
            }

            SafePlayerPrefs.SetInt(CompletedKey, 1);
            SafePlayerPrefs.Save();
        }
    }
}
