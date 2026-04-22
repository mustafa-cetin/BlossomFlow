using System;
using BlossomFlow.Application.Platform.CrazyGames;
using CrazyGames;
using UnityEngine;

namespace BlossomFlow.Application.Audio
{
    public sealed class CrazyGamesAudioMuteProvider : IExternalAudioMuteProvider, IDisposable
    {
        private readonly CrazyGamesSdkConfiguration _configuration;
        private bool _isSettingsListenerRegistered;

        public event Action Changed;

        public CrazyGamesAudioMuteProvider(CrazyGamesSdkConfiguration configuration)
        {
            _configuration = configuration;
            Initialize();
        }

        public bool IsAudioForcedMuted { get; private set; }

        public void Dispose()
        {
            if (!_isSettingsListenerRegistered || CrazySDK.IsShutDown)
            {
                return;
            }

            CrazySDK.Game.RemoveSettingsChangeListener(OnSettingsChanged);
            _isSettingsListenerRegistered = false;
        }

        private void Initialize()
        {
            if (_configuration == null ||
                !_configuration.IsEnabled ||
                !CrazySDK.IsAvailable ||
                CrazySDK.IsShutDown)
            {
                return;
            }

            try
            {
                CrazySDK.Init(OnCrazyGamesInitialized);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"CrazySDK audio settings could not be initialized. {exception.Message}");
            }
        }

        private void OnCrazyGamesInitialized()
        {
            TryRefreshCurrentSettings();

            if (_isSettingsListenerRegistered)
            {
                return;
            }

            try
            {
                CrazySDK.Game.AddSettingsChangeListener(OnSettingsChanged);
                _isSettingsListenerRegistered = true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"CrazySDK audio settings listener could not be registered. {exception.Message}");
            }
        }

        private void OnSettingsChanged(GameSettings settings)
        {
            ApplySettings(settings);
        }

        private void TryRefreshCurrentSettings()
        {
            try
            {
                ApplySettings(CrazySDK.Game.Settings);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"CrazySDK audio settings could not be read. {exception.Message}");
            }
        }

        private void ApplySettings(GameSettings settings)
        {
            var shouldForceMute = settings != null && settings.muteAudio;
            if (IsAudioForcedMuted == shouldForceMute)
            {
                return;
            }

            IsAudioForcedMuted = shouldForceMute;
            Changed?.Invoke();
        }
    }
}
