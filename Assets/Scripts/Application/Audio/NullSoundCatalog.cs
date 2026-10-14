using UnityEngine;

namespace BlossomFlow.Application.Audio
{
    public sealed class NullSoundCatalog : ISoundCatalog, IMusicCatalog
    {
        public bool TryGetClip(SoundCueId cueId, out AudioClip clip)
        {
            clip = null;
            return false;
        }

        public bool TryGetClip(MusicTrackId trackId, out AudioClip clip)
        {
            clip = null;
            return false;
        }
    }
}
