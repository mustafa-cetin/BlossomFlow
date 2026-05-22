using System;
using BlossomFlow.Application.Platform.CrazyGames;

namespace BlossomFlow.Application.Audio
{
    public sealed class CrazyGamesAudioMuteProvider : IExternalAudioMuteProvider, IDisposable
    {
        public event Action Changed;

        public CrazyGamesAudioMuteProvider(CrazyGamesSdkConfiguration configuration)
        {
        }

        public bool IsAudioForcedMuted { get; private set; }

        public void Dispose()
        {
        }
    }
}
