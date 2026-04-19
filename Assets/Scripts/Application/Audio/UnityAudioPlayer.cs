using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BlossomFlow.Application.Audio
{
    public sealed class UnityAudioPlayer : IAudioPlayer, IMusicPlayer, IDisposable
    {
        private readonly ISoundCatalog _soundCatalog;
        private readonly IMusicCatalog _musicCatalog;
        private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _loadedClipHandles = new();
        private AudioSource _soundEffectsSource;
        private AudioSource _musicSource;
        private GameObject _audioRoot;
        private float _soundEffectsVolume;
        private float _musicVolume;
        private bool _areSoundEffectsMuted;
        private bool _isMusicMuted;
        private bool _isDisposed;
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
            if (_areSoundEffectsMuted ||
                _soundCatalog == null ||
                !_soundCatalog.TryGetClipReference(cueId, out var clipReference))
            {
                return;
            }

            var clampedVolumeScale = Mathf.Clamp01(volumeScale);
            LoadClip(clipReference, clip => PlayLoadedSoundEffect(clip, clampedVolumeScale));
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
            if (_musicCatalog == null || !_musicCatalog.TryGetClipReference(trackId, out var clipReference))
            {
                return;
            }

            _currentMusicTrackId = trackId;
            LoadClip(clipReference, clip => PlayLoadedMusic(trackId, clip));
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
            _isDisposed = true;
            if (_audioRoot != null)
            {
                UnityEngine.Object.Destroy(_audioRoot);
            }

            ReleaseLoadedClips();
            _audioRoot = null;
            _soundEffectsSource = null;
            _musicSource = null;
        }

        private void PlayLoadedSoundEffect(AudioClip clip, float volumeScale)
        {
            if (_isDisposed || _areSoundEffectsMuted || clip == null)
            {
                return;
            }

            var source = GetOrCreateSoundEffectsSource();
            if (source == null)
            {
                return;
            }

            source.PlayOneShot(clip, volumeScale * _soundEffectsVolume);
        }

        private void PlayLoadedMusic(MusicTrackId trackId, AudioClip clip)
        {
            if (_isDisposed || _currentMusicTrackId != trackId || clip == null)
            {
                return;
            }

            var source = GetOrCreateMusicSource();
            if (source == null)
            {
                return;
            }

            if (source.clip == clip && source.isPlaying)
            {
                source.volume = GetMusicSourceVolume();
                return;
            }

            source.clip = clip;
            source.loop = true;
            source.volume = GetMusicSourceVolume();
            source.Play();
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

        private void LoadClip(AssetReferenceT<AudioClip> clipReference, Action<AudioClip> onLoaded)
        {
            if (_isDisposed || clipReference == null || !clipReference.RuntimeKeyIsValid())
            {
                return;
            }

            var cacheKey = GetReferenceCacheKey(clipReference);
            if (_loadedClipHandles.TryGetValue(cacheKey, out var cachedHandle))
            {
                if (cachedHandle.IsValid() && cachedHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    onLoaded?.Invoke(cachedHandle.Result);
                    return;
                }

                if (cachedHandle.IsValid() && !cachedHandle.IsDone)
                {
                    cachedHandle.Completed += handle => OnClipLoadCompleted(cacheKey, handle, onLoaded);
                    return;
                }

                _loadedClipHandles.Remove(cacheKey);
            }

            var handle = clipReference.LoadAssetAsync();
            _loadedClipHandles[cacheKey] = handle;
            handle.Completed += completedHandle => OnClipLoadCompleted(cacheKey, completedHandle, onLoaded);
        }

        private void OnClipLoadCompleted(
            string cacheKey,
            AsyncOperationHandle<AudioClip> handle,
            Action<AudioClip> onLoaded)
        {
            if (_isDisposed)
            {
                return;
            }

            if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
            {
                _loadedClipHandles.Remove(cacheKey);
                return;
            }

            _loadedClipHandles[cacheKey] = handle;
            onLoaded?.Invoke(handle.Result);
        }

        private void ReleaseLoadedClips()
        {
            foreach (var handle in _loadedClipHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            _loadedClipHandles.Clear();
        }

        private static string GetReferenceCacheKey(AssetReferenceT<AudioClip> clipReference)
        {
            return !string.IsNullOrEmpty(clipReference.AssetGUID)
                ? clipReference.AssetGUID
                : clipReference.RuntimeKey.ToString();
        }
    }
}
