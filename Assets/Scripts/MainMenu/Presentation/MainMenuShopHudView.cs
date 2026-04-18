using System;
using BlossomFlow.Application.Events;
using BlossomFlow.MainMenu.Domain;
using Reflex.Attributes;
using TMPro;
using UnityEngine;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MainMenuShopHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text goldText;

        private IMainMenuPlayerProgressService _progressService;
        private IEventSubscriber _eventSubscriber;
        private IDisposable _progressChangedSubscription;

        [Inject]
        private void Construct(
            IMainMenuPlayerProgressService progressService,
            IEventSubscriber eventSubscriber)
        {
            _progressService = progressService;
            _eventSubscriber = eventSubscriber;

            if (isActiveAndEnabled)
            {
                Subscribe();
                Refresh();
            }
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            _progressChangedSubscription?.Dispose();
            _progressChangedSubscription = null;
        }

        private void Subscribe()
        {
            if (_eventSubscriber == null || _progressChangedSubscription != null)
            {
                return;
            }

            _progressChangedSubscription = _eventSubscriber.Subscribe<MainMenuPlayerProgressChanged>(_ => Refresh());
        }

        private void Refresh()
        {
            if (_progressService == null || goldText == null)
            {
                return;
            }

            goldText.text = _progressService.Current.Gold.ToString();
        }
    }
}
