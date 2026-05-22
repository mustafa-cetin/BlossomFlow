namespace BlossomFlow.MainMenu.Domain
{
    public readonly struct MainMenuGardenPlantDefinition
    {
        public MainMenuGardenPlantDefinition(int plantId, int harvestGoldReward)
        {
            PlantId = plantId;
            HarvestGoldReward = harvestGoldReward;
        }

        public int PlantId { get; }
        public int HarvestGoldReward { get; }

        public static MainMenuGardenPlantDefinition Default => new(0, 50);
    }
}
