using TMPro;
using UnityEngine;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MainMenuFriendsPlaceholderView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private string title = "Garden";
        [SerializeField] private string message = "Magical seed ile cicek ek.";

        private void OnEnable()
        {
            ApplyCopy();
        }

        private void OnValidate()
        {
            ApplyCopy();
        }

        private void ApplyCopy()
        {
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (messageText != null)
            {
                messageText.text = message;
            }
        }
    }
}
