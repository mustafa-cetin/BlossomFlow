namespace BlossomFlow.Gameplay.Domain.Levels.Generation
{
    public interface IProceduralPotQueueGenerator
    {
        PotQueueColumnDefinition[] Generate(FlowerCellDefinition[] flowers, ProceduralLevelGenerationSettings settings, System.Random random);
    }
}
