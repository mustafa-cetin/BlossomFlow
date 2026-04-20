namespace BlossomFlow.MainMenu.DailyLogin
{
    public readonly struct DailyLoginBonusClaimed
    {
        public DailyLoginBonusClaimed(int dayIndex, DailyLoginReward reward)
        {
            DayIndex = dayIndex;
            Reward = reward;
        }

        public int DayIndex { get; }
        public DailyLoginReward Reward { get; }
    }
}
