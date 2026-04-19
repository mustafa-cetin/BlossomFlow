namespace BlossomFlow.Application.Tutorial
{
    public interface IFirstLevelTutorialProgressService
    {
        bool IsCompleted { get; }
        void MarkCompleted();
    }
}
