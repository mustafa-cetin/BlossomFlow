using System;

namespace BlossomFlow.Gameplay.Domain.Levels.Generation
{
    public sealed class ProceduralLevelDefinitionGenerator : ILevelDefinitionGenerator
    {
        private readonly IProceduralFlowerGridGenerator flowerGridGenerator;
        private readonly IProceduralPotQueueGenerator potQueueGenerator;

        public ProceduralLevelDefinitionGenerator()
            : this(new BalancedRandomFlowerGridGenerator(), new ColorMatchedPotQueueGenerator())
        {
        }

        public ProceduralLevelDefinitionGenerator(
            IProceduralFlowerGridGenerator flowerGridGenerator,
            IProceduralPotQueueGenerator potQueueGenerator)
        {
            this.flowerGridGenerator = flowerGridGenerator ?? throw new ArgumentNullException(nameof(flowerGridGenerator));
            this.potQueueGenerator = potQueueGenerator ?? throw new ArgumentNullException(nameof(potQueueGenerator));
        }

        public LevelDefinition Generate(ProceduralLevelGenerationSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.Validate();

            var random = settings.UseSeed ? new Random(settings.Seed) : new Random();
            var flowers = flowerGridGenerator.Generate(settings, random);
            var launchColumns = potQueueGenerator.Generate(flowers, settings, random);

            var definition = new LevelDefinition(
                settings.Width,
                settings.Height,
                flowers,
                launchColumns,
                settings.WaitingSlotCount,
                settings.ConveyorPath ?? new ConveyorPathDefinition("greenhouse"),
                Array.Empty<FlowerBlockerDefinition>(),
                settings.ConveyorMaxPots,
                settings.Difficulty);
            definition.Validate();
            return definition;
        }
    }
}
