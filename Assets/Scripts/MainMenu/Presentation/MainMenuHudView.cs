using System;
using BlossomFlow.Application.Audio;
using BlossomFlow.Application.Events;
using BlossomFlow.MainMenu.Domain;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MainMenuHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text magicalSeedText;
        [SerializeField] private TMP_Text countdownText;
        [Header("Audio Toggles")]
        [SerializeField] private Button soundEffectsButton;
        [SerializeField] private Button musicButton;
        [SerializeField] private string soundEffectsOnText = "SFX On";
        [SerializeField] private string soundEffectsOffText = "SFX Off";
        [SerializeField] private string musicOnText = "Music On";
        [SerializeField] private string musicOffText = "Music Off";
        [SerializeField] private Image soundEffectsIcon;
        [SerializeField] private Image musicIcon;
        [SerializeField] private Sprite soundEffectsOnSprite;
        [SerializeField] private Sprite soundEffectsOffSprite;
        [SerializeField] private Sprite musicOnSprite;
        [SerializeField] private Sprite musicOffSprite;

        private const float CountdownRefreshSeconds = 1f;
        private const string MaxLivesCountdownText = "MAX";

        private IMainMenuPlayerProgressService _progressService;
        private IEventSubscriber _eventSubscriber;
        private IEventPublisher _eventPublisher;
        private IAudioSettingsService _audioSettingsService;
        private IDisposable _progressChangedSubscription;
        private bool _isAudioSettingsSubscribed;
        private float _countdownRefreshTimer;

        [Inject]
        private void Construct(
            IMainMenuPlayerProgressService progressService,
            IEventSubscriber eventSubscriber,
            IEventPublisher eventPublisher,
            IAudioSettingsService audioSettingsService)
        {
            _progressService = progressService;
            _eventSubscriber = eventSubscriber;
            _eventPublisher = eventPublisher;
            _audioSettingsService = audioSettingsService;

            if (isActiveAndEnabled)
            {
                Subscribe();
                SubscribeAudioSettings();
                Refresh();
            }
        }

        private void OnEnable()
        {
            _countdownRefreshTimer = 0f;
            AddButtonListeners();
            Subscribe();
            SubscribeAudioSettings();
            Refresh();
        }

        private void Update()
        {
            if (_progressService == null)
            {
                return;
            }

            _countdownRefreshTimer += Time.unscaledDeltaTime;
            if (_countdownRefreshTimer < CountdownRefreshSeconds)
            {
                return;
            }

            _countdownRefreshTimer = 0f;
            Refresh();
        }

        private void OnDisable()
        {
            RemoveButtonListeners();
            _progressChangedSubscription?.Dispose();
            _progressChangedSubscription = null;
            UnsubscribeAudioSettings();
        }

        private void AddButtonListeners()
        {
            if (soundEffectsButton != null)
            {
                soundEffectsButton.onClick.AddListener(ToggleSoundEffects);
            }

            if (musicButton != null)
            {
                musicButton.onClick.AddListener(ToggleMusic);
            }
        }

        private void RemoveButtonListeners()
        {
            if (soundEffectsButton != null)
            {
                soundEffectsButton.onClick.RemoveListener(ToggleSoundEffects);
            }

            if (musicButton != null)
            {
                musicButton.onClick.RemoveListener(ToggleMusic);
            }
        }

        private void Subscribe()
        {
            if (_eventSubscriber == null || _progressChangedSubscription != null)
            {
                return;
            }

            _progressChangedSubscription = _eventSubscriber.Subscribe<MainMenuPlayerProgressChanged>(_ => Refresh());
        }

        private void SubscribeAudioSettings()
        {
            if (_audioSettingsService == null || _isAudioSettingsSubscribed)
            {
                return;
            }

            _audioSettingsService.Changed += RefreshAudioButtons;
            _isAudioSettingsSubscribed = true;
        }

        private void UnsubscribeAudioSettings()
        {
            if (_audioSettingsService == null || !_isAudioSettingsSubscribed)
            {
                return;
            }

            _audioSettingsService.Changed -= RefreshAudioButtons;
            _isAudioSettingsSubscribed = false;
        }

        private void Refresh()
        {
            RefreshProgress();
            RefreshAudioButtons();
        }

        private void RefreshProgress()
        {
            if (_progressService == null)
            {
                return;
            }

            var progress = _progressService.Current;
            if (livesText != null)
            {
                livesText.text = $"{progress.CurrentLives}";
            }

            if (goldText != null)
            {
                goldText.text = progress.Gold.ToString();
            }

            if (magicalSeedText != null)
            {
                magicalSeedText.text = progress.MagicalSeed.ToString();
            }

            if (countdownText != null)
            {
                var hasMaxLives = progress.CurrentLives >= progress.MaxLives;
                countdownText.text = hasMaxLives ? MaxLivesCountdownText : progress.NextLifeCountdown;
                countdownText.gameObject.SetActive(hasMaxLives || progress.HasLifeCountdown);
            }
        }

        private void ToggleSoundEffects()
        {
            if (_audioSettingsService == null || _audioSettingsService.IsAudioForcedMuted)
            {
                RefreshAudioButtons();
                return;
            }

            if (!_audioSettingsService.AreSoundEffectsEnabled)
            {
                _audioSettingsService.ToggleSoundEffects();
                PlayButtonClickSound();
            }
            else
            {
                PlayButtonClickSound();
                _audioSettingsService.ToggleSoundEffects();
            }

            RefreshAudioButtons();
        }

        private void ToggleMusic()
        {
            if (_audioSettingsService == null || _audioSettingsService.IsAudioForcedMuted)
            {
                RefreshAudioButtons();
                return;
            }

            PlayButtonClickSound();
            _audioSettingsService.ToggleMusic();
            RefreshAudioButtons();
        }

        private void RefreshAudioButtons()
        {
            if (_audioSettingsService == null)
            {
                return;
            }

            var canEditAudio = !_audioSettingsService.IsAudioForcedMuted;
            if (soundEffectsButton != null)
            {
                soundEffectsButton.interactable = canEditAudio;
            }

            if (musicButton != null)
            {
                musicButton.interactable = canEditAudio;
            }

            ApplyToggleVisuals(
                soundEffectsIcon,
                _audioSettingsService.AreSoundEffectsEnabled,
                soundEffectsOnText,
                soundEffectsOffText,
                soundEffectsOnSprite,
                soundEffectsOffSprite);

            ApplyToggleVisuals(
                musicIcon,
                _audioSettingsService.IsMusicEnabled,
                musicOnText,
                musicOffText,
                musicOnSprite,
                musicOffSprite);
        }

        private static void ApplyToggleVisuals(
            Image icon,
            bool isEnabled,
            string enabledText,
            string disabledText,
            Sprite enabledSprite,
            Sprite disabledSprite)
        {
            if (icon == null)
            {
                return;
            }

            var sprite = isEnabled ? enabledSprite : disabledSprite;
            if (sprite != null)
            {
                icon.sprite = sprite;
            }
        }

        private void PlayButtonClickSound()
        {
            _eventPublisher?.Publish(new SoundRequested(SoundCueId.ButtonClick));
        }
    }
}
