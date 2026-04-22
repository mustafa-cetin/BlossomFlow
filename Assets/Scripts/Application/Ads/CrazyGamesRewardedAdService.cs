using System;
using BlossomFlow.Application.Platform.CrazyGames;
using CrazyGames;
using UnityEngine;

namespace BlossomFlow.Application.Ads
{
    public sealed class CrazyGamesRewardedAdService : IRewardedAdService
    {
        private readonly CrazyGamesSdkConfiguration _configuration;

        public CrazyGamesRewardedAdService(CrazyGamesSdkConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ShowRewardedAd(Action onRewarded, Action onFailed)
        {
            if (_configuration == null || !_configuration.IsEnabled || !CrazySDK.IsAvailable)
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
