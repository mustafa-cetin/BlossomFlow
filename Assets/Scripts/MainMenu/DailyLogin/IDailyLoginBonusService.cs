namespace BlossomFlow.MainMenu.DailyLogin
{
    public interface IDailyLoginBonusService
    {
        DailyLoginBonusState GetState(DailyLoginReward[] rewards);
        bool TryClaim(DailyLoginReward[] rewards, out DailyLoginBonusState state);
    }
}
