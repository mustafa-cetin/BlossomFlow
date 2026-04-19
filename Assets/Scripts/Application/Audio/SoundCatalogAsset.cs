using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BlossomFlow.Application.Audio
{
    [CreateAssetMenu(fileName = "SoundCatalog", menuName = "Blossom Flow/Audio/Sound Catalog")]
    public sealed class SoundCatalogAsset : ScriptableObject, ISoundCatalog, IMusicCatalog
    {
        [Header("Sound Effects")]
        [SerializeField] private SoundCueBinding[] cues = Array.Empty<SoundCueBinding>();

        [Header("Music")]
        [SerializeField] private MusicTrackBinding[] musicTracks = Array.Empty<MusicTrackBinding>();

        public bool TryGetClipReference(SoundCueId cueId, out AssetReferenceT<AudioClip> clipReference)
        {
            for (var i = 0; i < cues.Length; i++)
            {
                if (cues[i].CueId != cueId || !IsValidReference(cues[i].ClipReference))
                {
                    continue;
                }

                clipReference = cues[i].ClipReference;
                return true;
            }

            clipReference = null;
            return false;
        }

        public bool TryGetClipReference(MusicTrackId trackId, out AssetReferenceT<AudioClip> clipReference)
        {
            for (var i = 0; i < musicTracks.Length; i++)
            {
                if (musicTracks[i].TrackId != trackId || !IsValidReference(musicTracks[i].ClipReference))
                {
                    continue;
                }

                clipReference = musicTracks[i].ClipReference;
                return true;
            }

            clipReference = null;
            return false;
        }

        private static bool IsValidReference(AssetReferenceT<AudioClip> clipReference)
        {
            return clipReference != null && clipReference.RuntimeKeyIsValid();
        }

        [Serializable]
        private sealed class SoundCueBinding
        {
            [SerializeField] private SoundCueId cueId;
            [SerializeField] private AssetReferenceT<AudioClip> clipReference;

            public SoundCueId CueId => cueId;
            public AssetReferenceT<AudioClip> ClipReference => clipReference;
        }

        [Serializable]
        private sealed class MusicTrackBinding
        {
            [SerializeField] private MusicTrackId trackId;
            [SerializeField] private AssetReferenceT<AudioClip> clipReference;

            public MusicTrackId TrackId => trackId;
            public AssetReferenceT<AudioClip> ClipReference => clipReference;
        }
    }
}
