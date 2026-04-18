using System;
using BlossomFlow.Application.Events;
using BlossomFlow.MainMenu.Domain;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MainMenuBottomNavigationView : MonoBehaviour
    {
        [Serializable]
        private struct TabButtonBinding
        {
            public MainMenuTab tab;
            public Button button;
            public Image targetImage;
            public Sprite normalSprite;
            public Sprite selectedSprite;
            public RectTransform visualRoot;
            public TMP_Text selectedLabel;
            public string labelText;
        }

        [SerializeField] private TabButtonBinding[] tabButtons;
        [SerializeField, Min(0f)] private float selectedHeightIncrease = 28f;

        private IMainMenuNavigator _navigator;
        private IEventSubscriber _eventSubscriber;
        private IDisposable _tabChangedSubscription;
        private ButtonLayoutState[] _layoutStates;

        private struct ButtonLayoutState
        {
            public RectTransform rectTransform;
            public Vector2 sizeDelta;
            public Vector2 anchoredPosition;
            public Vector3 localScale;
        }

        [Inject]
        private void Construct(IMainMenuNavigator navigator, IEventSubscriber eventSubscriber)
        {
            _navigator = navigator;
            _eventSubscriber = eventSubscriber;

            if (isActiveAndEnabled)
            {
                Subscribe();
                RefreshSelection();
            }
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshSelection();
        }

        private void OnDisable()
        {
            _tabChangedSubscription?.Dispose();
            _tabChangedSubscription = null;
        }

        private void Subscribe()
        {
            if (_eventSubscriber == null || _tabChangedSubscription != null)
            {
                return;
            }

            _tabChangedSubscription = _eventSubscriber.Subscribe<MainMenuTabChanged>(_ => RefreshSelection());
        }

        private void RefreshSelection()
        {
            if (_navigator == null || tabButtons == null)
            {
                return;
            }

            EnsureLayoutStates();

            for (var i = 0; i < tabButtons.Length; i++)
            {
                var binding = tabButtons[i];
                var isSelected = binding.tab == _navigator.CurrentTab;
                if (binding.button != null)
                {
                    binding.button.interactable = !isSelected;
                }

                ApplyButtonSprite(binding, isSelected);
                ApplyButtonLayout(binding, i, isSelected);
                ApplyButtonLabel(binding, isSelected);
            }
        }

        private static void ApplyButtonSprite(TabButtonBinding binding, bool isSelected)
        {
            var targetImage = binding.targetImage;
            if (targetImage == null && binding.button != null)
            {
                targetImage = binding.button.image;
            }

            if (targetImage == null)
            {
                return;
            }

            var nextSprite = isSelected ? binding.selectedSprite : binding.normalSprite;
            if (nextSprite != null)
            {
                targetImage.sprite = nextSprite;
            }
        }

        private void EnsureLayoutStates()
        {
            if (_layoutStates != null && _layoutStates.Length == tabButtons.Length)
            {
                return;
            }

            _layoutStates = new ButtonLayoutState[tabButtons.Length];

            for (var i = 0; i < tabButtons.Length; i++)
            {
                var rectTransform = GetVisualRoot(tabButtons[i]);
                if (rectTransform == null)
                {
                    continue;
                }

                _layoutStates[i] = new ButtonLayoutState
                {
                    rectTransform = rectTransform,
                    sizeDelta = rectTransform.sizeDelta,
                    anchoredPosition = rectTransform.anchoredPosition,
                    localScale = rectTransform.localScale
                };
            }
        }

        private void ApplyButtonLayout(TabButtonBinding binding, int index, bool isSelected)
        {
            if (_layoutStates == null || index < 0 || index >= _layoutStates.Length)
            {
                return;
            }

            var state = _layoutStates[index];
            var rectTransform = state.rectTransform != null ? state.rectTransform : GetVisualRoot(binding);
            if (rectTransform == null)
            {
                return;
            }

            var selectedHeightDelta = isSelected ? selectedHeightIncrease : 0f;
            var sizeDelta = state.sizeDelta;
            sizeDelta.y += selectedHeightDelta;

            rectTransform.localScale = state.localScale;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = state.anchoredPosition + new Vector2(0f, rectTransform.pivot.y * selectedHeightDelta);
        }

        private static void ApplyButtonLabel(TabButtonBinding binding, bool isSelected)
        {
            if (binding.selectedLabel == null)
            {
                return;
            }

            binding.selectedLabel.text = string.IsNullOrWhiteSpace(binding.labelText) ? binding.tab.ToString() : binding.labelText;
            binding.selectedLabel.gameObject.SetActive(isSelected);
        }

        private static RectTransform GetVisualRoot(TabButtonBinding binding)
        {
            if (binding.visualRoot != null)
            {
                return binding.visualRoot;
            }

            return binding.button != null ? binding.button.transform as RectTransform : null;
        }
    }
}
