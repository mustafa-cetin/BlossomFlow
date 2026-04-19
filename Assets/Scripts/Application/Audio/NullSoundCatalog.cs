using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BlossomFlow.Application.Audio
{
    public sealed class NullSoundCatalog : ISoundCatalog, IMusicCatalog
    {
        public bool TryGetClipReference(SoundCueId cueId, out AssetReferenceT<AudioClip> clipReference)
        {
            clipReference = null;
            return false;
        }

        public bool TryGetClipReference(MusicTrackId trackId, out AssetReferenceT<AudioClip> clipReference)
        {
            clipReference = null;
            return false;
        }
    }
}
