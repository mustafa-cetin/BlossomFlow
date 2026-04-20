using System;

namespace BlossomFlow.Application.Audio
{
    public interface IAudioSettingsService
    {
        event Action Changed;

        bool AreSoundEffectsEnabled { get; }
        bool IsMusicEnabled { get; }
        bool IsAudioForcedMuted { get; }

        void SetSoundEffectsEnabled(bool isEnabled);
        void SetMusicEnabled(bool isEnabled);
        void ToggleSoundEffects();
        void ToggleMusic();
    }
}
