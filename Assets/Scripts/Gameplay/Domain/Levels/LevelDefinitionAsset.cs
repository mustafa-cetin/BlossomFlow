using UnityEngine;

namespace BlossomFlow.Gameplay.Domain.Levels
{
    [CreateAssetMenu(
        fileName = "LevelDefinition",
        menuName = "Blossom Flow/Level Definition")]
    public sealed class LevelDefinitionAsset : ScriptableObject
    {
        [SerializeField] private LevelDefinition definition = new();

        public LevelDefinition Definition
        {
            get
            {
                EnsureDefinition();
                return definition;
            }
        }

        public void SetDefinition(LevelDefinition newDefinition)
        {
            definition = newDefinition ?? new LevelDefinition();
            EnsureDefinition();
        }

        private void OnValidate()
        {
            EnsureDefinition();
        }

        private void EnsureDefinition()
        {
            definition ??= new LevelDefinition();
            definition.LaunchColumns ??= new PotQueueColumnDefinition[LevelDefinition.DefaultLaunchColumnCount];
            if (definition.LaunchColumns.Length < LevelDefinition.MinLaunchColumnCount)
            {
                definition.LaunchColumns = new PotQueueColumnDefinition[LevelDefinition.DefaultLaunchColumnCount];
            }

            for (var i = 0; i < definition.LaunchColumns.Length; i++)
            {
                definition.LaunchColumns[i] ??= new PotQueueColumnDefinition();
                definition.LaunchColumns[i].Pots ??= System.Array.Empty<PotDefinition>();
            }

            definition.Flowers ??= System.Array.Empty<FlowerCellDefinition>();
            definition.Blockers ??= System.Array.Empty<FlowerBlockerDefinition>();
            definition.ConveyorPath ??= new ConveyorPathDefinition("greenhouse");
            definition.Width = Mathf.Max(1, definition.Width);
            definition.Height = Mathf.Max(1, definition.Height);
            definition.WaitingSlotCount = Mathf.Max(1, definition.WaitingSlotCount);
            definition.MagicalSeedReward = Mathf.Max(0, definition.MagicalSeedReward);
            definition.ConveyorMaxPots = definition.ConveyorMaxPots <= 0
                ? LevelDefinition.DefaultConveyorMaxPots
                : definition.ConveyorMaxPots;
        }
    }
}
