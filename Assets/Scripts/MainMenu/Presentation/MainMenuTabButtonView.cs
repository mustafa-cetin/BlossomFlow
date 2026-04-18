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
        private IMainMenuNavigator _navigator;

        [Inject]
        private void Construct(IMainMenuNavigator navigator)
        {
            _navigator = navigator;
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
            _navigator?.TrySelect(tab);
        }
    }
}
