using System;

namespace BlossomFlow.MainMenu.DailyLogin
{
    [Serializable]
    public readonly struct DailyLoginReward
    {
        public DailyLoginReward(DailyLoginRewardType type, int amount)
        {
            Type = type;
            Amount = Math.Max(0, amount);
        }

        public DailyLoginRewardType Type { get; }
        public int Amount { get; }
        public bool IsEmpty => Amount <= 0;
    }
}
