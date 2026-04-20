using System;
using BlossomFlow.Application.Audio;
using BlossomFlow.Application.Events;
using DG.Tweening;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BlossomFlow.MainMenu.DailyLogin
{
    [DisallowMultipleComponent]
    public sealed class DailyLoginBonusView : MonoBehaviour
    {
        [Serializable]
        private struct RewardDefinition
        {
            public DailyLoginRewardType type;
            [Min(0)] public int amount;
        }

        [Serializable]
        private struct DayBinding
        {
            public GameObject root;
            public TMP_Text dayText;
            public TMP_Text rewardText;
            public Button dayButton;
            public Image rewardIcon;
            public Sprite goldSprite;
            public Sprite magicalSeedSprite;
            public GameObject claimedTodayMarker;
            public Sprite currentMarkerSprite;
            public Sprite defaultMarkerSprite;
            //public GameObject currentMarker;
        }

        [Header("Rewards")]
        [SerializeField] private RewardDefinition[] rewards =
        {
            new RewardDefinition { type = DailyLoginRewardType.Gold, amount = 50 },
            new RewardDefinition { type = DailyLoginRewardType.Gold, amount = 75 },
            new RewardDefinition { type = DailyLoginRewardType.Gold, amount = 100 },
            new RewardDefinition { type = DailyLoginRewardType.MagicalSeed, amount = 1 },
            new RewardDefinition { type = DailyLoginRewardType.Gold, amount = 150 },
            new RewardDefinition { type = DailyLoginRewardType.Gold, amount = 200 },
            new RewardDefinition { type = DailyLoginRewardType.MagicalSeed, amount = 2 }
        };

        [Header("Bindings")]
        [SerializeField] private Button openButton;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private DayBinding[] dayBindings;
        [SerializeField] private Button claimButton;
        [SerializeField] private TMP_Text claimButtonText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private bool hidePanelOnEnable = true;
        [SerializeField] private string claimAvailableText = "Claim";
        [SerializeField] private string claimUnavailableText = "Claimed";
        [SerializeField] private string availableStatusFormat = "Day {0} reward ready";
        [SerializeField] private string unavailableStatusFormat = "Come back tomorrow for Day {0}";
        [SerializeField] private string dayFormat = "Day {0}";
        [SerializeField] private string goldRewardFormat = "+{0} Gold";
        [SerializeField] private string magicalSeedRewardFormat = "+{0} Seed";
        [Header("Open Button Attention")]
        [SerializeField, Min(0f)] private float attentionShakeDegrees = 10f;
        [SerializeField, Min(0.01f)] private float attentionShakeSeconds = 0.45f;
        [SerializeField, Min(0f)] private float attentionPauseSeconds = 2f;
        [SerializeField, Min(1)] private int attentionShakeVibrato = 12;

        private IDailyLoginBonusService _dailyLoginBonusService;
        private IEventPublisher _eventPublisher;
        private UnityAction[] _dayClaimActions;
        private Sequence _attentionSequence;
        private Quaternion _openButtonInitialRotation;
        private bool _hasOpenButtonInitialRotation;

        [Inject]
        private void Construct(
            IDailyLoginBonusService dailyLoginBonusService,
            IEventPublisher eventPublisher)
        {
            _dailyLoginBonusService = dailyLoginBonusService;
            _eventPublisher = eventPublisher;

            if (isActiveAndEnabled)
            {
                Refresh();
            }
        }

        private void OnEnable()
        {
            if (hidePanelOnEnable && panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (openButton != null)
            {
                CaptureOpenButtonRotation();
                openButton.onClick.AddListener(OpenPanel);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }

            if (claimButton != null)
            {
                claimButton.onClick.AddListener(Claim);
            }

            AddDayButtonListeners();
            Refresh();
        }

        private void OnDisable()
        {
            StopOpenButtonAttention();

            if (openButton != null)
            {
                openButton.onClick.RemoveListener(OpenPanel);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
            }

            if (claimButton != null)
            {
                claimButton.onClick.RemoveListener(Claim);
            }

            RemoveDayButtonListeners();
        }

        private void OnValidate()
        {
            RefreshStaticBindings();
        }

        private void OpenPanel()
        {
            PlayButtonClickSound();
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            Refresh();
        }

        private void ClosePanel()
        {
            PlayButtonClickSound();
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            Refresh();
        }

        private void AddDayButtonListeners()
        {
            if (dayBindings == null)
            {
                return;
            }

            _dayClaimActions = new UnityAction[dayBindings.Length];
            for (var i = 0; i < dayBindings.Length; i++)
            {
                if (dayBindings[i].dayButton == null)
                {
                    continue;
                }

                _dayClaimActions[i] = ClaimCurrentDay;
                dayBindings[i].dayButton.onClick.AddListener(_dayClaimActions[i]);
            }
        }

        private void RemoveDayButtonListeners()
        {
            if (dayBindings == null || _dayClaimActions == null)
            {
                return;
            }

            var count = Mathf.Min(dayBindings.Length, _dayClaimActions.Length);
            for (var i = 0; i < count; i++)
            {
                if (dayBindings[i].dayButton == null || _dayClaimActions[i] == null)
                {
                    continue;
                }

                dayBindings[i].dayButton.onClick.RemoveListener(_dayClaimActions[i]);
            }

            _dayClaimActions = null;
        }

        private void ClaimCurrentDay()
        {
            Claim();
        }

        private void Claim()
        {
            if (_dailyLoginBonusService == null)
            {
                return;
            }

            if (_dailyLoginBonusService.TryClaim(CreateRewards(), out _))
            {
                PlayButtonClickSound();
            }

            Refresh();
        }

        private void Refresh()
        {
            if (_dailyLoginBonusService == null)
            {
                RefreshStaticBindings();
                return;
            }

            var state = _dailyLoginBonusService.GetState(CreateRewards());
            RefreshDayBindings(state);
            RefreshClaimState(state);
            RefreshOpenButtonAttention(state);
        }

        private void RefreshStaticBindings()
        {
            var staticState = new DailyLoginBonusState(1, 7, true, 0, default);
            RefreshDayBindings(staticState);
        }

        private void RefreshDayBindings(DailyLoginBonusState state)
        {
            if (dayBindings == null)
            {
                return;
            }

            var definedRewards = CreateRewards();
            for (var i = 0; i < dayBindings.Length; i++)
            {
                var dayIndex = i + 1;
                var isInsideCycle = dayIndex <= state.MaxDayCount;
                var binding = dayBindings[i];
                if (binding.root != null)
                {
                    binding.root.SetActive(isInsideCycle);
                }

                if (!isInsideCycle)
                {
                    continue;
                }

                var reward = GetReward(definedRewards, dayIndex);
                if (binding.dayText != null)
                {
                    binding.dayText.text = string.Format(dayFormat, dayIndex);
                }

                if (binding.rewardText != null)
                {
                    binding.rewardText.text = FormatReward(reward);
                }

                if (binding.dayButton != null)
                {
                    binding.dayButton.interactable = state.CanClaim && dayIndex == state.CurrentDayIndex;
                }

                if (binding.rewardIcon != null)
                {
                    var sprite = reward.Type == DailyLoginRewardType.MagicalSeed
                        ? binding.magicalSeedSprite
                        : binding.goldSprite;
                    if (sprite != null)
                    {
                        binding.rewardIcon.sprite = sprite;
                    }
                }

                if (binding.currentMarkerSprite != null)
                {
                    binding.root.GetComponent<Image>().sprite = dayIndex == state.CurrentDayIndex ? binding.currentMarkerSprite : binding.defaultMarkerSprite;
                   // binding.currentMarker.SetActive(dayIndex == state.CurrentDayIndex);
                }

                if (binding.claimedTodayMarker != null)
                {
                    binding.claimedTodayMarker.SetActive(dayIndex == state.ClaimedDayIndex);
                }
            }
        }

        private void RefreshOpenButtonAttention(DailyLoginBonusState state)
        {
            if (state.CanClaim)
            {
                StartOpenButtonAttention();
                return;
            }

            StopOpenButtonAttention();
        }

        private void StartOpenButtonAttention()
        {
            if (openButton == null || _attentionSequence != null)
            {
                return;
            }

            CaptureOpenButtonRotation();
            var target = openButton.transform;
            _attentionSequence = DOTween.Sequence()
                .Append(target.DOPunchRotation(
                    new Vector3(0f, 0f, attentionShakeDegrees),
                    attentionShakeSeconds,
                    attentionShakeVibrato,
                    0.75f))
                .AppendInterval(attentionPauseSeconds)
                .SetLoops(-1, LoopType.Restart)
                .SetLink(openButton.gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void StopOpenButtonAttention()
        {
            _attentionSequence?.Kill();
            _attentionSequence = null;

            if (openButton != null && _hasOpenButtonInitialRotation)
            {
                openButton.transform.localRotation = _openButtonInitialRotation;
            }
        }

        private void CaptureOpenButtonRotation()
        {
            if (openButton == null || _hasOpenButtonInitialRotation)
            {
                return;
            }

            _openButtonInitialRotation = openButton.transform.localRotation;
            _hasOpenButtonInitialRotation = true;
        }

        private void PlayButtonClickSound()
        {
            _eventPublisher?.Publish(new SoundRequested(SoundCueId.ButtonClick));
        }

        private void RefreshClaimState(DailyLoginBonusState state)
        {
            if (claimButton != null)
            {
                claimButton.interactable = state.CanClaim;
            }

            if (claimButtonText != null)
            {
                claimButtonText.text = state.CanClaim ? claimAvailableText : claimUnavailableText;
            }

            if (statusText != null)
            {
                var format = state.CanClaim ? availableStatusFormat : unavailableStatusFormat;
                statusText.text = string.Format(format, state.CurrentDayIndex);
            }
        }

        private DailyLoginReward[] CreateRewards()
        {
            if (rewards == null || rewards.Length == 0)
            {
                return Array.Empty<DailyLoginReward>();
            }

            var count = Mathf.Min(7, rewards.Length);
            var result = new DailyLoginReward[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = new DailyLoginReward(rewards[i].type, rewards[i].amount);
            }

            return result;
        }

        private static DailyLoginReward GetReward(DailyLoginReward[] definedRewards, int dayIndex)
        {
            if (definedRewards == null || definedRewards.Length == 0)
            {
                return new DailyLoginReward(DailyLoginRewardType.Gold, 0);
            }

            var rewardIndex = Mathf.Clamp(dayIndex - 1, 0, definedRewards.Length - 1);
            return definedRewards[rewardIndex];
        }

        private string FormatReward(DailyLoginReward reward)
        {
            var format = reward.Type == DailyLoginRewardType.MagicalSeed
                ? magicalSeedRewardFormat
                : goldRewardFormat;
            return string.Format(format, reward.Amount);
        }
    }
}
