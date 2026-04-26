using System;
using UnityEngine;

namespace BlossomFlow.Application.Audio
{
    [CreateAssetMenu(fileName = "SoundCatalog", menuName = "Blossom Flow/Audio/Sound Catalog")]
    public sealed class SoundCatalogAsset : ScriptableObject, ISoundCatalog, IMusicCatalog
    {
        [Header("Sound Effects")]
        [SerializeField] private SoundCueBinding[] cues = Array.Empty<SoundCueBinding>();

        [Header("Music")]
        [SerializeField] private MusicTrackBinding[] musicTracks = Array.Empty<MusicTrackBinding>();

        public bool TryGetClip(SoundCueId cueId, out AudioClip clip)
        {
            for (var i = 0; i < cues.Length; i++)
            {
                if (cues[i].CueId != cueId || cues[i].Clip == null)
                {
                    continue;
                }

                clip = cues[i].Clip;
                return true;
            }

            clip = null;
            return false;
        }

        public bool TryGetClip(MusicTrackId trackId, out AudioClip clip)
        {
            for (var i = 0; i < musicTracks.Length; i++)
            {
                if (musicTracks[i].TrackId != trackId || musicTracks[i].Clip == null)
                {
                    continue;
                }

                clip = musicTracks[i].Clip;
                return true;
            }

            clip = null;
            return false;
        }

        [Serializable]
        private sealed class SoundCueBinding
        {
            [SerializeField] private SoundCueId cueId;
            [SerializeField] private AudioClip clip;

            public SoundCueId CueId => cueId;
            public AudioClip Clip => clip;
        }

        [Serializable]
        private sealed class MusicTrackBinding
        {
            [SerializeField] private MusicTrackId trackId;
            [SerializeField] private AudioClip clip;

            public MusicTrackId TrackId => trackId;
            public AudioClip Clip => clip;
        }
    }
}
