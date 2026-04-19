using BlossomFlow.Application.Audio;
using BlossomFlow.Application.Events;
using BlossomFlow.MainMenu.Domain;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class MainMenuTabButtonView : MonoBehaviour
    {
        [SerializeField] private MainMenuTab tab;

        private Button _button;
        private IEventPublisher _eventPublisher;
        private IMainMenuNavigator _navigator;

        [Inject]
        private void Construct(IMainMenuNavigator navigator, IEventPublisher eventPublisher)
        {
            _navigator = navigator;
            _eventPublisher = eventPublisher;
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(SelectTab);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(SelectTab);
            }
        }

        private void SelectTab()
        {
            if (_navigator != null && _navigator.TrySelect(tab))
            {
                _eventPublisher?.Publish(new SoundRequested(SoundCueId.ButtonClick));
            }
        }
    }
}
