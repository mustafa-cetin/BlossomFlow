namespace BlossomFlow.Application.Audio
{
    public interface IMusicPlayer
    {
        void PlayLoop(MusicTrackId trackId);
        void Stop();
        void SetMusicVolume(float volume);
        void SetMusicMuted(bool isMuted);
    }
}
