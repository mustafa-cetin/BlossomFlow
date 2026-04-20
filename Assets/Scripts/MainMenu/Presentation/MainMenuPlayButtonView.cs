using System;
using BlossomFlow.Application.Audio;
using BlossomFlow.Application.Events;
using BlossomFlow.Application.Levels;
using BlossomFlow.Gameplay.Domain.Levels;
using BlossomFlow.MainMenu.Domain;
using DG.Tweening;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class MainMenuPlayButtonView : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private string labelFormat = "Level {0}";
        [SerializeField] private TMP_Text difficultyLabelText;
        [SerializeField] private Graphic buttonGraphic;
        [SerializeField] private Color normalButtonColor = Color.white;
        [SerializeField] private Color hardButtonColor = new Color(1f, 0.56f, 0.18f, 1f);
        [SerializeField] private Color veryHardButtonColor = new Color(0.9f, 0.16f, 0.16f, 1f);
        [SerializeField] private string hardLabel = "Hard";
        [SerializeField] private string veryHardLabel = "Very Hard";
        [SerializeField] private float breathingScale = 1.05f;
        [SerializeField] private float breathingSeconds = 0.9f;
        [SerializeField] private Ease breathingEase = Ease.InOutSine;

        private Button _button;
        private IEventPublisher _eventPublisher;
        private IEventSubscriber _eventSubscriber;
        private IMainMenuPlayerProgressService _progressService;
        private ILevelCatalog _levelCatalog;
        private IDisposable _progressChangedSubscription;
        private IDisposable _difficultyLoadRequest;
        private Tween _breathingTween;
        private Vector3 _initialScale;
        private int _loadingDifficultyLevelNumber;
        private int _loadedDifficultyLevelNumber;
        private LevelDifficulty _loadedDifficulty = LevelDifficulty.Normal;

        [Inject]
        private void Construct(
            IEventPublisher eventPublisher,
            IEventSubscriber eventSubscriber,
            IMainMenuPlayerProgressService progressService,
            ILevelCatalog levelCatalog)
        {
            _eventPublisher = eventPublisher;
            _eventSubscriber = eventSubscriber;
            _progressService = progressService;
            _levelCatalog = levelCatalog;

            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            RefreshLabel();
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            buttonGraphic ??= _button.targetGraphic;
            _initialScale = transform.localScale;
            RefreshLabel();
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(RequestPlay);
            }

            StartBreathing();
            Subscribe();
            RefreshLabel();
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(RequestPlay);
            }

            StopBreathing();
            _progressChangedSubscription?.Dispose();
            _progressChangedSubscription = null;
            _difficultyLoadRequest?.Dispose();
            _difficultyLoadRequest = null;
        }

        private void RequestPlay()
        {
            if (_eventPublisher == null || _progressService == null)
            {
                return;
            }

            var progress = _progressService.Current;
            if (progress.CurrentLives <= 0)
            {
                RefreshLabel();
                return;
            }

            _eventPublisher.Publish(new SoundRequested(SoundCueId.ButtonClick));
            _eventPublisher.Publish(new MainMenuPlayRequested(progress.LevelNumber));
        }

        private void Subscribe()
        {
            if (_eventSubscriber == null || _progressChangedSubscription != null)
            {
                return;
            }

            _progressChangedSubscription = _eventSubscriber.Subscribe<MainMenuPlayerProgressChanged>(_ => RefreshLabel());
        }

        private void RefreshLabel()
        {
            if (_progressService == null)
            {
                return;
            }

            var progress = _progressService.Current;
            var difficulty = GetDifficulty(progress.LevelNumber);
            var difficultyText = GetDifficultyText(difficulty);
            if (labelText != null)
            {
                var levelText = string.Format(labelFormat, progress.LevelNumber);
                labelText.text = difficultyLabelText == null && !string.IsNullOrEmpty(difficultyText)
                    ? $"{levelText}\n{difficultyText}"
                    : levelText;
            }

            if (difficultyLabelText != null)
            {
                difficultyLabelText.text = difficultyText;
                difficultyLabelText.gameObject.SetActive(!string.IsNullOrEmpty(difficultyText));
            }

            ApplyButtonColor(difficulty);

            if (_button != null)
            {
                _button.interactable = progress.CurrentLives > 0;
            }
        }

        private LevelDifficulty GetDifficulty(int levelNumber)
        {
            if (_levelCatalog == null)
            {
                return LevelDifficulty.Normal;
            }

            try
            {
                if (_levelCatalog.TryGetDefinition(levelNumber, out var definition))
                {
                    CacheDifficulty(levelNumber, definition);
                    return definition.Difficulty;
                }
            }
            catch (Exception)
            {
                return LevelDifficulty.Normal;
            }

            if (_loadedDifficultyLevelNumber == levelNumber)
            {
                return _loadedDifficulty;
            }

            StartDifficultyLoad(levelNumber);
            return LevelDifficulty.Normal;
        }

        private void StartDifficultyLoad(int levelNumber)
        {
            if (_loadingDifficultyLevelNumber == levelNumber && _difficultyLoadRequest != null)
            {
                return;
            }

            _difficultyLoadRequest?.Dispose();
            _difficultyLoadRequest = _levelCatalog.LoadDefinition(
                levelNumber,
                definition =>
                {
                    CacheDifficulty(levelNumber, definition);
                    _difficultyLoadRequest = null;
                    RefreshLabel();
                },
                _ =>
                {
                    _difficultyLoadRequest = null;
                });
            _loadingDifficultyLevelNumber = levelNumber;
        }

        private void CacheDifficulty(int levelNumber, LevelDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            _loadedDifficultyLevelNumber = levelNumber;
            _loadedDifficulty = definition.Difficulty;
        }

        private string GetDifficultyText(LevelDifficulty difficulty)
        {
            return difficulty switch
            {
                LevelDifficulty.Hard => hardLabel,
                LevelDifficulty.VeryHard => veryHardLabel,
                _ => string.Empty
            };
        }

        private Color GetButtonColor(LevelDifficulty difficulty)
        {
            return difficulty switch
            {
                LevelDifficulty.Hard => hardButtonColor,
                LevelDifficulty.VeryHard => veryHardButtonColor,
                _ => normalButtonColor
            };
        }

        private void ApplyButtonColor(LevelDifficulty difficulty)
        {
            var buttonColor = GetButtonColor(difficulty);
            if (_button != null)
            {
                var colors = _button.colors;
                colors.normalColor = buttonColor;
                colors.highlightedColor = Color.Lerp(buttonColor, Color.white, 0.16f);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = Color.Lerp(buttonColor, Color.black, 0.2f);
                _button.colors = colors;
            }

            if (buttonGraphic != null)
            {
                buttonGraphic.color = buttonColor;
            }
        }

        private void StartBreathing()
        {
            StopBreathing();
            transform.localScale = _initialScale;
            _breathingTween = transform.DOScale(_initialScale * breathingScale, breathingSeconds)
                .SetEase(breathingEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void StopBreathing()
        {
            _breathingTween?.Kill();
            _breathingTween = null;
            transform.localScale = _initialScale;
        }
    }
}
