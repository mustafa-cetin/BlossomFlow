using System;
using BlossomFlow.Application.Events;
using BlossomFlow.MainMenu.Domain;
using DG.Tweening;
using Reflex.Attributes;
using UnityEngine;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MainMenuTabTransitionView : MonoBehaviour
    {
        [Serializable]
        private struct TabPanelBinding
        {
            public MainMenuTab tab;
            public RectTransform panel;
        }

        [SerializeField] private RectTransform viewport;
        [SerializeField] private TabPanelBinding[] panels;
        [SerializeField] private float transitionSeconds = 0.28f;
        [SerializeField] private Ease transitionEase = Ease.OutCubic;

        private IMainMenuNavigator _navigator;
        private IEventSubscriber _eventSubscriber;
        private IDisposable _tabChangedSubscription;
        private Sequence _transitionSequence;

        [Inject]
        private void Construct(IMainMenuNavigator navigator, IEventSubscriber eventSubscriber)
        {
            _navigator = navigator;
            _eventSubscriber = eventSubscriber;

            if (isActiveAndEnabled)
            {
                Subscribe();
                SnapToCurrentTab();
            }
        }

        private void Awake()
        {
            if (viewport == null)
            {
                viewport = transform as RectTransform;
            }
        }

        private void OnEnable()
        {
            Subscribe();
            SnapToCurrentTab();
        }

        private void OnDisable()
        {
            _tabChangedSubscription?.Dispose();
            _tabChangedSubscription = null;
            KillTransition();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            SnapToCurrentTab();
        }

        private void Subscribe()
        {
            if (_eventSubscriber == null || _tabChangedSubscription != null)
            {
                return;
            }

            _tabChangedSubscription = _eventSubscriber.Subscribe<MainMenuTabChanged>(PlayTransition);
        }

        private void SnapToCurrentTab()
        {
            if (_navigator == null)
            {
                return;
            }

            KillTransition();
            for (var i = 0; i < panels.Length; i++)
            {
                if (panels[i].panel == null)
                {
                    continue;
                }

                panels[i].panel.anchoredPosition = GetAnchoredPosition(panels[i].tab);
                panels[i].panel.gameObject.SetActive(panels[i].tab == _navigator.CurrentTab);
            }
        }

        private void PlayTransition(MainMenuTabChanged tabChanged)
        {
            var previousPanel = GetPanel(tabChanged.PreviousTab);
            var currentPanel = GetPanel(tabChanged.CurrentTab);
            if (currentPanel == null)
            {
                SnapToCurrentTab();
                return;
            }

            KillTransition();

            var width = GetViewportWidth();
            var direction = tabChanged.Direction >= 0 ? 1 : -1;
            var incomingStart = new Vector2(width * direction, 0f);
            var outgoingEnd = new Vector2(-width * direction, 0f);

            currentPanel.gameObject.SetActive(true);
            currentPanel.anchoredPosition = incomingStart;

            _transitionSequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (previousPanel != null)
            {
                previousPanel.gameObject.SetActive(true);
                previousPanel.anchoredPosition = Vector2.zero;
                _transitionSequence.Join(previousPanel.DOAnchorPos(outgoingEnd, transitionSeconds).SetEase(transitionEase));
            }

            _transitionSequence.Join(currentPanel.DOAnchorPos(Vector2.zero, transitionSeconds).SetEase(transitionEase));
            _transitionSequence.OnComplete(SnapToCurrentTab);
        }

        private RectTransform GetPanel(MainMenuTab tab)
        {
            for (var i = 0; i < panels.Length; i++)
            {
                if (panels[i].tab == tab)
                {
                    return panels[i].panel;
                }
            }

            return null;
        }

        private Vector2 GetAnchoredPosition(MainMenuTab tab)
        {
            return tab == _navigator.CurrentTab
                ? Vector2.zero
                : new Vector2(((int)tab - (int)_navigator.CurrentTab) * GetViewportWidth(), 0f);
        }

        private float GetViewportWidth()
        {
            if (viewport == null)
            {
                return Screen.width;
            }

            return Mathf.Max(1f, viewport.rect.width);
        }

        private void KillTransition()
        {
            _transitionSequence?.Kill();
            _transitionSequence = null;
        }
    }
}
