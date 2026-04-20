using System;
using BlossomFlow.Application.Events;
using BlossomFlow.Application.Levels;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlossomFlow.Application.SceneLoading
{
    [DisallowMultipleComponent]
    public sealed class LoadingCatalogErrorPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button retryButton;
        [SerializeField] private string defaultMessage = "Levels could not be loaded. Please check your connection and try again.";
        [SerializeField] private string retryingMessage = "Trying to connect again...";
        [SerializeField] private bool hideOnAwake = true;

        private IEventSubscriber _eventSubscriber;
        private IRemoteLevelCatalogPreloader _remoteLevelCatalogPreloader;
        private IDisposable _catalogUnavailableSubscription;
        private bool _isShowing;

        [Inject]
        private void Construct(
            IEventSubscriber eventSubscriber,
            IRemoteLevelCatalogPreloader remoteLevelCatalogPreloader)
        {
            _eventSubscriber = eventSubscriber;
            _remoteLevelCatalogPreloader = remoteLevelCatalogPreloader;

            if (isActiveAndEnabled)
            {
                Subscribe();
                RefreshFromCatalogStatus();
            }
        }

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (panelRoot == gameObject && panelCanvasGroup == null)
            {
                panelCanvasGroup = GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null)
                {
                    panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (hideOnAwake && !_isShowing)
            {
                Hide();
            }
            else if (_isShowing)
            {
                SetPanelVisible(true);
            }
        }

        private void OnEnable()
        {
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(Retry);
            }

            Subscribe();
            RefreshFromCatalogStatus();
        }

        private void OnDisable()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(Retry);
            }

            _catalogUnavailableSubscription?.Dispose();
            _catalogUnavailableSubscription = null;
        }

        private void Subscribe()
        {
            if (_eventSubscriber == null || _catalogUnavailableSubscription != null)
            {
                return;
            }

            _catalogUnavailableSubscription = _eventSubscriber.Subscribe<RemoteLevelCatalogUnavailable>(
                unavailable => Show(unavailable.Reason));
        }

        private void RefreshFromCatalogStatus()
        {
            if (_remoteLevelCatalogPreloader == null || !_remoteLevelCatalogPreloader.IsUnavailable)
            {
                return;
            }

            Show(_remoteLevelCatalogPreloader.UnavailableReason);
        }

        private void Show(string reason)
        {
            _isShowing = true;
            SetPanelVisible(true);

            if (messageText != null)
            {
                messageText.text = string.IsNullOrWhiteSpace(reason) ? defaultMessage : reason;
            }

            SetRetryInteractable(true);
        }

        private void Hide()
        {
            _isShowing = false;
            SetPanelVisible(false);

            if (messageText != null)
            {
                messageText.text = defaultMessage;
            }

            SetRetryInteractable(true);
        }

        private void Retry()
        {
            if (_remoteLevelCatalogPreloader == null)
            {
                return;
            }

            SetRetryInteractable(false);
            if (messageText != null)
            {
                messageText.text = retryingMessage;
            }

            _remoteLevelCatalogPreloader.RetryLoad();
            StartCoroutine(WaitForRetryResult());
        }

        private System.Collections.IEnumerator WaitForRetryResult()
        {
            while (_remoteLevelCatalogPreloader != null &&
                !_remoteLevelCatalogPreloader.IsLoaded &&
                !_remoteLevelCatalogPreloader.IsUnavailable)
            {
                yield return null;
            }

            if (_remoteLevelCatalogPreloader != null && _remoteLevelCatalogPreloader.IsLoaded)
            {
                Hide();
                yield break;
            }

            SetRetryInteractable(true);
        }

        private void SetRetryInteractable(bool isInteractable)
        {
            if (retryButton != null)
            {
                retryButton.interactable = isInteractable;
            }
        }

        private void SetPanelVisible(bool isVisible)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = isVisible ? 1f : 0f;
                panelCanvasGroup.interactable = isVisible;
                panelCanvasGroup.blocksRaycasts = isVisible;
                return;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(isVisible);
            }
        }
    }
}
