using System;
using BlossomFlow.Application.Events;

namespace BlossomFlow.Application.Audio
{
    public sealed class SoundEventController : IDisposable
    {
        private readonly IAudioPlayer _audioPlayer;
        private readonly IDisposable _soundRequestedSubscription;

        public SoundEventController(IEventSubscriber eventSubscriber, IAudioPlayer audioPlayer)
        {
            if (eventSubscriber == null)
            {
                throw new ArgumentNullException(nameof(eventSubscriber));
            }

            _audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
            _soundRequestedSubscription = eventSubscriber.Subscribe<SoundRequested>(OnSoundRequested);
        }

        public void Dispose()
        {
            _soundRequestedSubscription?.Dispose();
        }

        private void OnSoundRequested(SoundRequested soundRequested)
        {
            _audioPlayer.Play(soundRequested.CueId, soundRequested.VolumeScale);
        }
    }
}
