namespace BlossomFlow.Application.Audio
{
    public interface IAudioPlayer
    {
        void Play(SoundCueId cueId);
        void Play(SoundCueId cueId, float volumeScale);
        void SetSoundEffectsVolume(float volume);
        void SetSoundEffectsMuted(bool isMuted);
    }
}
