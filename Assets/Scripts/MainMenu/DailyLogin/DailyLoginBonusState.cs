namespace BlossomFlow.MainMenu.DailyLogin
{
    public readonly struct DailyLoginBonusState
    {
        public DailyLoginBonusState(
            int currentDayIndex,
            int maxDayCount,
            bool canClaim,
            int claimedDayIndex,
            DailyLoginReward currentReward)
        {
            CurrentDayIndex = currentDayIndex;
            MaxDayCount = maxDayCount;
            CanClaim = canClaim;
            ClaimedDayIndex = claimedDayIndex;
            CurrentReward = currentReward;
        }

        public int CurrentDayIndex { get; }
        public int MaxDayCount { get; }
        public bool CanClaim { get; }
        public int ClaimedDayIndex { get; }
        public DailyLoginReward CurrentReward { get; }
    }
}
