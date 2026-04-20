using System;
using System.Globalization;
using BlossomFlow.Application.Events;
using BlossomFlow.MainMenu.Domain;

namespace BlossomFlow.MainMenu.DailyLogin
{
    public sealed class DailyLoginBonusService : IDailyLoginBonusService
    {
        private const int MaxDayCount = 7;
        private const string LastClaimDayNumberKey = "blossom_flow.daily_login.last_claim_day_number";
        private const string CurrentDayIndexKey = "blossom_flow.daily_login.current_day_index";

        private readonly IMainMenuPlayerProgressService _progressService;
        private readonly IEventPublisher _eventPublisher;

        public DailyLoginBonusService(
            IMainMenuPlayerProgressService progressService,
            IEventPublisher eventPublisher)
        {
            _progressService = progressService;
            _eventPublisher = eventPublisher;
        }

        public DailyLoginBonusState GetState(DailyLoginReward[] rewards)
        {
            var normalizedRewards = NormalizeRewards(rewards);
            var currentDayIndex = ReadCurrentDayIndex();
            return CreateState(currentDayIndex, CanClaimToday(), normalizedRewards);
        }

        public bool TryClaim(DailyLoginReward[] rewards, out DailyLoginBonusState state)
        {
            var normalizedRewards = NormalizeRewards(rewards);
            var currentDayIndex = ReadCurrentDayIndex();
            if (!CanClaimToday())
            {
                state = CreateState(currentDayIndex, false, normalizedRewards);
                return false;
            }

            var reward = GetReward(normalizedRewards, currentDayIndex);
            if (reward.IsEmpty)
            {
                state = CreateState(currentDayIndex, false, normalizedRewards);
                return false;
            }

            GrantReward(reward);

            var nextDayIndex = currentDayIndex >= MaxDayCount ? 1 : currentDayIndex + 1;
            SafePlayerPrefs.SetInt(CurrentDayIndexKey, nextDayIndex);
            SafePlayerPrefs.SetString(LastClaimDayNumberKey, GetCurrentDayNumber().ToString(CultureInfo.InvariantCulture));
            SafePlayerPrefs.Save();

            _eventPublisher.Publish(new DailyLoginBonusClaimed(currentDayIndex, reward));
            state = CreateState(nextDayIndex, false, normalizedRewards);
            return true;
        }

        private void GrantReward(DailyLoginReward reward)
        {
            if (reward.IsEmpty)
            {
                return;
            }

            switch (reward.Type)
            {
                case DailyLoginRewardType.Gold:
                    _progressService.AddGold(reward.Amount);
                    break;
                case DailyLoginRewardType.MagicalSeed:
                    _progressService.AddMagicalSeed(reward.Amount);
                    break;
            }
        }

        private static DailyLoginBonusState CreateState(
            int currentDayIndex,
            bool canClaim,
            DailyLoginReward[] rewards)
        {
            var reward = GetReward(rewards, currentDayIndex);
            return new DailyLoginBonusState(
                currentDayIndex,
                MaxDayCount,
                canClaim && !reward.IsEmpty,
                canClaim ? 0 : GetPreviousDayIndex(currentDayIndex),
                reward);
        }

        private static int GetPreviousDayIndex(int currentDayIndex)
        {
            return currentDayIndex <= 1 ? MaxDayCount : currentDayIndex - 1;
        }

        private static DailyLoginReward GetReward(DailyLoginReward[] rewards, int dayIndex)
        {
            if (rewards.Length == 0)
            {
                return new DailyLoginReward(DailyLoginRewardType.Gold, 0);
            }

            var rewardIndex = Math.Min(MaxDayCount, Math.Max(1, dayIndex)) - 1;
            rewardIndex = Math.Min(rewardIndex, rewards.Length - 1);
            return rewards[rewardIndex];
        }

        private static DailyLoginReward[] NormalizeRewards(DailyLoginReward[] rewards)
        {
            if (rewards == null || rewards.Length == 0)
            {
                return Array.Empty<DailyLoginReward>();
            }

            var count = Math.Min(MaxDayCount, rewards.Length);
            var normalized = new DailyLoginReward[count];
            for (var i = 0; i < count; i++)
            {
                normalized[i] = new DailyLoginReward(rewards[i].Type, rewards[i].Amount);
            }

            return normalized;
        }

        private static int ReadCurrentDayIndex()
        {
            return Math.Min(MaxDayCount, Math.Max(1, SafePlayerPrefs.GetInt(CurrentDayIndexKey, 1)));
        }

        private static bool CanClaimToday()
        {
            var lastClaimDayNumber = ReadLong(LastClaimDayNumberKey, long.MinValue);
            return lastClaimDayNumber < GetCurrentDayNumber();
        }

        private static long GetCurrentDayNumber()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerDay;
        }

        private static long ReadLong(string key, long defaultValue)
        {
            var value = SafePlayerPrefs.GetString(key, string.Empty);
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue)
                ? parsedValue
                : defaultValue;
        }
    }
}
