using System;
using CrazyGames;
using UnityEngine;

namespace BlossomFlow.Application.Ads
{
    public sealed class CrazyGamesRewardedAdService : IRewardedAdService
    {
        public void ShowRewardedAd(Action onRewarded, Action onFailed)
        {
            if (!CrazySDK.IsAvailable)
            {
                onFailed?.Invoke();
                return;
            }

            void RequestAd()
            {
                CrazySDK.Ad.RequestAd(
                    CrazyAdType.Rewarded,
                    null,
                    error =>
                    {
                        Debug.LogWarning($"Rewarded ad failed: {error}");
                        onFailed?.Invoke();
                    },
                    () => onRewarded?.Invoke());
            }

            if (CrazySDK.IsInitialized)
            {
                RequestAd();
                return;
            }

            CrazySDK.Init(RequestAd);
        }
    }
}
