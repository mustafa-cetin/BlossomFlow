namespace BlossomFlow.Gameplay.Domain.Levels.Generation
{
    public interface ILevelDefinitionGenerator
    {
        LevelDefinition Generate(ProceduralLevelGenerationSettings settings);
    }
}
