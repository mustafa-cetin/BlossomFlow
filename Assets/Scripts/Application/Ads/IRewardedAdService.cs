using System;

namespace BlossomFlow.Application.Ads
{
    public interface IRewardedAdService
    {
        void ShowRewardedAd(Action onRewarded, Action onFailed);
    }
}
