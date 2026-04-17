namespace BlossomFlow.Gameplay.Domain.Levels
{
    public interface ILevelSessionFactory
    {
        LevelSession Create(LevelDefinition definition);
    }
}
