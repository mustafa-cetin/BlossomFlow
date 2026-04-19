namespace BlossomFlow.Application.Tutorial
{
    public interface IGardenTutorialProgressService
    {
        bool IsCompleted { get; }
        void MarkCompleted();
    }
}
