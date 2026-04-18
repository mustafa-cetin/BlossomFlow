using System;
using TMPro;
using UnityEngine;

namespace BlossomFlow.MainMenu.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MainMenuShopOfferListView : MonoBehaviour
    {
        [Serializable]
        private struct OfferBinding
        {
            public TMP_Text titleText;
            public TMP_Text descriptionText;
            public TMP_Text priceText;
            public string title;
            public string description;
            public string price;
        }

        [SerializeField] private OfferBinding[] offers;

        private void OnEnable()
        {
            RefreshOffers();
        }

        private void OnValidate()
        {
            RefreshOffers();
        }

        private void RefreshOffers()
        {
            if (offers == null)
            {
                return;
            }

            for (var i = 0; i < offers.Length; i++)
            {
                if (offers[i].titleText != null)
                {
                    offers[i].titleText.text = offers[i].title;
                }

                if (offers[i].descriptionText != null)
                {
                    offers[i].descriptionText.text = offers[i].description;
                }

                if (offers[i].priceText != null)
                {
                    offers[i].priceText.text = offers[i].price;
                }
            }
        }
    }
}
