using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BlossomFlow.Application.Audio
{
    public interface ISoundCatalog
    {
        bool TryGetClipReference(SoundCueId cueId, out AssetReferenceT<AudioClip> clipReference);
    }
}
