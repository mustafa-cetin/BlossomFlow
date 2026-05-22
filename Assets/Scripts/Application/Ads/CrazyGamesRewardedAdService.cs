using System;
using BlossomFlow.Application.Platform.CrazyGames;

namespace BlossomFlow.Application.Ads
{
    public sealed class CrazyGamesRewardedAdService : IRewardedAdService
    {
        public CrazyGamesRewardedAdService(CrazyGamesSdkConfiguration configuration)
        {
        }

        public void ShowRewardedAd(Action onRewarded, Action onFailed)
        {
            onFailed?.Invoke();
        }
    }
}
