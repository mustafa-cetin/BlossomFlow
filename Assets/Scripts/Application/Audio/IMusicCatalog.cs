using UnityEngine;

namespace BlossomFlow.Application.Audio
{
    public interface IMusicCatalog
    {
        bool TryGetClip(MusicTrackId trackId, out AudioClip clip);
    }
}
