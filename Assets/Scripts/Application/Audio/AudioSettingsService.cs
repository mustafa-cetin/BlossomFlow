using System;
using UnityEngine;

namespace BlossomFlow.Application.Audio
{
    public sealed class AudioSettingsService : IAudioSettingsService, IDisposable
    {
        private const string SoundEffectsEnabledKey = "Audio.SoundEffects.Enabled";
        private const string MusicEnabledKey = "Audio.Music.Enabled";
        private const string AudioDefaultsMigrationKey = "Audio.DefaultsMigration.Version";
        private const int AudioDefaultsMigrationVersion = 1;
        private const int EnabledValue = 1;
        private const int DisabledValue = 0;

        private readonly IAudioPlayer _audioPlayer;
        private readonly IMusicPlayer _musicPlayer;
        private readonly IExternalAudioMuteProvider _externalAudioMuteProvider;
        private bool _areSoundEffectsEnabledByPlayer;
        private bool _isMusicEnabledByPlayer;

        public event Action Changed;

        public AudioSettingsService(
            IAudioPlayer audioPlayer,
            IMusicPlayer musicPlayer,
            IExternalAudioMuteProvider externalAudioMuteProvider)
        {
            _audioPlayer = audioPlayer;
            _musicPlayer = musicPlayer;
            _externalAudioMuteProvider = externalAudioMuteProvider;

            MigrateAudioDefaults();

            _areSoundEffectsEnabledByPlayer = PlayerPrefs.GetInt(SoundEffectsEnabledKey, EnabledValue) == EnabledValue;
            _isMusicEnabledByPlayer = PlayerPrefs.GetInt(MusicEnabledKey, EnabledValue) == EnabledValue;

            if (_externalAudioMuteProvider != null)
            {
                _externalAudioMuteProvider.Changed += OnExternalAudioMuteChanged;
            }

            ApplySoundEffectsMute();
            ApplyMusicMute();
        }

        public bool AreSoundEffectsEnabled => !IsAudioForcedMuted && _areSoundEffectsEnabledByPlayer;
        public bool IsMusicEnabled => !IsAudioForcedMuted && _isMusicEnabledByPlayer;
        public bool IsAudioForcedMuted => _externalAudioMuteProvider != null && _externalAudioMuteProvider.IsAudioForcedMuted;

        public void SetSoundEffectsEnabled(bool isEnabled)
        {
            if (_areSoundEffectsEnabledByPlayer == isEnabled)
            {
                return;
            }

            _areSoundEffectsEnabledByPlayer = isEnabled;
            PlayerPrefs.SetInt(SoundEffectsEnabledKey, isEnabled ? EnabledValue : DisabledValue);
            PlayerPrefs.Save();
            ApplySoundEffectsMute();
            Changed?.Invoke();
        }

        public void SetMusicEnabled(bool isEnabled)
        {
            if (_isMusicEnabledByPlayer == isEnabled)
            {
                return;
            }

            _isMusicEnabledByPlayer = isEnabled;
            PlayerPrefs.SetInt(MusicEnabledKey, isEnabled ? EnabledValue : DisabledValue);
            PlayerPrefs.Save();
            ApplyMusicMute();
            Changed?.Invoke();
        }

        public void ToggleSoundEffects()
        {
            SetSoundEffectsEnabled(!AreSoundEffectsEnabled);
        }

        public void ToggleMusic()
        {
            SetMusicEnabled(!IsMusicEnabled);
        }

        public void Dispose()
        {
            if (_externalAudioMuteProvider != null)
            {
                _externalAudioMuteProvider.Changed -= OnExternalAudioMuteChanged;
            }
        }

        private void OnExternalAudioMuteChanged()
        {
            ApplySoundEffectsMute();
            ApplyMusicMute();
            Changed?.Invoke();
        }

        private static void MigrateAudioDefaults()
        {
            if (PlayerPrefs.GetInt(AudioDefaultsMigrationKey, 0) >= AudioDefaultsMigrationVersion)
            {
                return;
            }

            PlayerPrefs.SetInt(SoundEffectsEnabledKey, EnabledValue);
            PlayerPrefs.SetInt(MusicEnabledKey, EnabledValue);
            PlayerPrefs.SetInt(AudioDefaultsMigrationKey, AudioDefaultsMigrationVersion);
            PlayerPrefs.Save();
        }

        private void ApplySoundEffectsMute()
        {
            _audioPlayer?.SetSoundEffectsMuted(!AreSoundEffectsEnabled);
        }

        private void ApplyMusicMute()
        {
            _musicPlayer?.SetMusicMuted(!IsMusicEnabled);
        }
    }
}
