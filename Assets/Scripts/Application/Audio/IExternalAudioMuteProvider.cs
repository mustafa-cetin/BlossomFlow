using System;

namespace BlossomFlow.Application.Audio
{
    public interface IExternalAudioMuteProvider
    {
        event Action Changed;

        bool IsAudioForcedMuted { get; }
    }
}
