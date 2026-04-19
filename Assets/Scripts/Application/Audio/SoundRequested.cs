namespace BlossomFlow.Application.Audio
{
    public readonly struct SoundRequested
    {
        public SoundRequested(SoundCueId cueId)
        {
            CueId = cueId;
            VolumeScale = 1f;
        }

        public SoundRequested(SoundCueId cueId, float volumeScale)
        {
            CueId = cueId;
            VolumeScale = volumeScale;
        }

        public SoundCueId CueId { get; }
        public float VolumeScale { get; }
    }
}
