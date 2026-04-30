namespace BlossomFlow.Gameplay.Domain.Levels.Generation
{
    public interface IProceduralFlowerGridGenerator
    {
        FlowerCellDefinition[] Generate(ProceduralLevelGenerationSettings settings, System.Random random);
    }
}
