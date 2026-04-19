namespace BlossomFlow.Application.Audio
{
    public sealed class BackgroundMusicController
    {
        public BackgroundMusicController(IMusicPlayer musicPlayer, bool playOnStartup, MusicTrackId startupTrackId)
        {
            if (playOnStartup)
            {
                musicPlayer?.PlayLoop(startupTrackId);
            }
        }
    }
}
