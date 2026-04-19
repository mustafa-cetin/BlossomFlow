using System;
using UnityEngine;

namespace BlossomFlow.Application.Audio
{
    public sealed class UnityAudioPlayer : IAudioPlayer, IMusicPlayer, IDisposable
    {
        private readonly ISoundCatalog _soundCatalog;
        private readonly IMusicCatalog _musicCatalog;
        private AudioSource _soundEffectsSource;
        private AudioSource _musicSource;
        private GameObject _audioRoot;
        private float _soundEffectsVolume;
        private float _musicVolume;
        private bool _areSoundEffectsMuted;
        private bool _isMusicMuted;
        private MusicTrackId? _currentMusicTrackId;

        public UnityAudioPlayer(ISoundCatalog soundCatalog, IMusicCatalog musicCatalog, float soundEffectsVolume, float musicVolume)
        {
            _soundCatalog = soundCatalog;
            _musicCatalog = musicCatalog;
            _soundEffectsVolume = Mathf.Clamp01(soundEffectsVolume);
            _musicVolume = Mathf.Clamp01(musicVolume);
        }

        public void Play(SoundCueId cueId)
        {
            Play(cueId, 1f);
        }

        public void Play(SoundCueId cueId, float volumeScale)
        {
            if (_areSoundEffectsMuted || _soundCatalog == null || !_soundCatalog.TryGetClip(cueId, out var clip))
            {
                return;
            }

            var source = GetOrCreateSoundEffectsSource();
            if (source == null)
            {
                return;
            }

            source.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * _soundEffectsVolume);
        }

        public void SetSoundEffectsVolume(float volume)
        {
            _soundEffectsVolume = Mathf.Clamp01(volume);
        }

        public void SetSoundEffectsMuted(bool isMuted)
        {
            _areSoundEffectsMuted = isMuted;
        }

        public void PlayLoop(MusicTrackId trackId)
        {
            if (_musicCatalog == null || !_musicCatalog.TryGetClip(trackId, out var clip))
            {
                return;
            }

            var source = GetOrCreateMusicSource();
            if (source == null)
            {
                return;
            }

            if (_currentMusicTrackId == trackId && source.clip == clip && source.isPlaying)
            {
                return;
            }

            _currentMusicTrackId = trackId;
            source.clip = clip;
            source.loop = true;
            source.volume = GetMusicSourceVolume();
            source.Play();
        }

        public void Stop()
        {
            _currentMusicTrackId = null;
            if (_musicSource == null)
            {
                return;
            }

            _musicSource.Stop();
            _musicSource.clip = null;
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            if (_musicSource != null)
            {
                _musicSource.volume = GetMusicSourceVolume();
            }
        }

        public void SetMusicMuted(bool isMuted)
        {
            _isMusicMuted = isMuted;
            if (_musicSource != null)
            {
                _musicSource.volume = GetMusicSourceVolume();
            }
        }

        public void Dispose()
        {
            if (_audioRoot != null)
            {
                UnityEngine.Object.Destroy(_audioRoot);
            }

            _audioRoot = null;
            _soundEffectsSource = null;
            _musicSource = null;
        }

        private AudioSource GetOrCreateSoundEffectsSource()
        {
            if (_soundEffectsSource != null)
            {
                return _soundEffectsSource;
            }

            _soundEffectsSource = GetOrCreateAudioRoot().AddComponent<AudioSource>();
            _soundEffectsSource.playOnAwake = false;
            _soundEffectsSource.spatialBlend = 0f;
            return _soundEffectsSource;
        }

        private AudioSource GetOrCreateMusicSource()
        {
            if (_musicSource != null)
            {
                return _musicSource;
            }

            _musicSource = GetOrCreateAudioRoot().AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f;
            _musicSource.loop = true;
            _musicSource.volume = GetMusicSourceVolume();
            return _musicSource;
        }

        private GameObject GetOrCreateAudioRoot()
        {
            if (_audioRoot != null)
            {
                return _audioRoot;
            }

            _audioRoot = new GameObject("BlossomFlowAudioPlayer");
            UnityEngine.Object.DontDestroyOnLoad(_audioRoot);
            return _audioRoot;
        }

        private float GetMusicSourceVolume()
        {
            return _isMusicMuted ? 0f : _musicVolume;
        }
    }
}
