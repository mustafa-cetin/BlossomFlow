using System;
using BlossomFlow.Application.Events;
using BlossomFlow.MainMenu.Domain;
using Reflex.Attributes;
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
        }

        [SerializeField] private TabButtonBinding[] tabButtons;

        private IMainMenuNavigator _navigator;
        private IEventSubscriber _eventSubscriber;
        private IDisposable _tabChangedSubscription;

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

            for (var i = 0; i < tabButtons.Length; i++)
            {
                var isSelected = tabButtons[i].tab == _navigator.CurrentTab;
                if (tabButtons[i].button != null)
                {
                    tabButtons[i].button.interactable = !isSelected;
                }

                ApplyButtonSprite(tabButtons[i], isSelected);
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
    }
}
