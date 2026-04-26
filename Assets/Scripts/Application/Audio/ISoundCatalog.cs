using UnityEngine;

namespace BlossomFlow.Application.Audio
{
    public interface ISoundCatalog
    {
        bool TryGetClip(SoundCueId cueId, out AudioClip clip);
    }
}
