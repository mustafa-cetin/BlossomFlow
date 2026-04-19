using System;
using System.Linq;

namespace BlossomFlow.Gameplay.Domain.Levels
{
    public sealed class LevelSessionFactory : ILevelSessionFactory
    {
        public LevelSession Create(LevelDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            definition.Validate();

            var flowers = definition.Flowers.Select(flower =>
                new FlowerCell(new GridPosition(flower.X, flower.Y), flower.Color));

            var flowerGrid = new FlowerGrid(definition.Width, definition.Height, flowers);
            var waitingSlots = new WaitingSlots(definition.WaitingSlotCount);
            var launchColumns = definition.LaunchColumns.Select(column =>
                new PotQueueColumn(column.Select(pot => new PotModel(pot.Color, pot.Capacity, pot.LinkedPairId))));
            var conveyor = new ConveyorBelt(definition.CreateConveyorPath(), definition.ConveyorMaxPots);

            return new LevelSession(flowerGrid, waitingSlots, launchColumns, conveyor);
        }
    }
}
