using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BlossomFlow.Application.Audio
{
    public interface IMusicCatalog
    {
        bool TryGetClipReference(MusicTrackId trackId, out AssetReferenceT<AudioClip> clipReference);
    }
}
