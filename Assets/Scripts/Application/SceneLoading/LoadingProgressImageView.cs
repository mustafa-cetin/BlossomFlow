using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace BlossomFlow.Application.SceneLoading
{
    [DisallowMultipleComponent]
    public sealed class LoadingProgressImageView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;

        private ILoadingSceneController _loadingSceneController;

        [Inject]
        private void Construct(ILoadingSceneController loadingSceneController)
        {
            _loadingSceneController = loadingSceneController;
        }

        private void Awake()
        {
            if (fillImage == null)
            {
                fillImage = GetComponent<Image>();
            }

            SetProgress(0f);
        }

        private void Start()
        {
            if (_loadingSceneController == null)
            {
                Debug.LogError("LoadingProgressImageView needs Reflex injection. Add a ContainerScope to the Loading scene.");
                return;
            }

            StartCoroutine(_loadingSceneController.LoadPendingTargetAsync(SetProgress));
        }

        private void SetProgress(float progress)
        {
            if (fillImage == null)
            {
                return;
            }

            fillImage.fillAmount = Mathf.Clamp01(progress);
        }
    }
}
