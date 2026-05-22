using System;

namespace BlossomFlow.MainMenu.Domain
{
    public sealed class MainMenuGardenConfig
    {
        public int UnlockLevel { get; set; } = 20;
        public TimeSpan GrowthDuration { get; set; } = TimeSpan.FromHours(24d);
        public TimeSpan WaterReduction { get; set; } = TimeSpan.FromHours(1d);
        public TimeSpan WaterCooldown { get; set; } = TimeSpan.FromMinutes(10d);
        public int FreePlotCount { get; set; } = 3;
        public int PlotUnlockGoldCost { get; set; } = 200;
        public int PlotUnlockGoldCostIncrement { get; set; } = 100;
        public int HarvestGoldReward { get; set; } = 50;

        public MainMenuGardenTiming ToTiming()
        {
            return new MainMenuGardenTiming(GrowthDuration, WaterReduction, WaterCooldown);
        }
    }
}
