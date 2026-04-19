using System;

namespace BlossomFlow.MainMenu.Domain
{
    public readonly struct MainMenuGardenTiming
    {
        public MainMenuGardenTiming(TimeSpan growthDuration, TimeSpan waterReduction, TimeSpan waterCooldown)
        {
            GrowthDuration = growthDuration > TimeSpan.Zero ? growthDuration : TimeSpan.Zero;
            WaterReduction = waterReduction > TimeSpan.Zero ? waterReduction : TimeSpan.Zero;
            WaterCooldown = waterCooldown > TimeSpan.Zero ? waterCooldown : TimeSpan.Zero;
        }

        public TimeSpan GrowthDuration { get; }
        public TimeSpan WaterReduction { get; }
        public TimeSpan WaterCooldown { get; }
    }
}
