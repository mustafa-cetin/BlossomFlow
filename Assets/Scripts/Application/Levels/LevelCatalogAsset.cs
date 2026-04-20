using UnityEngine;
using UnityEngine.AddressableAssets;
using BlossomFlow.Gameplay.Domain.Levels;

namespace BlossomFlow.Application.Levels
{
    [CreateAssetMenu(
        fileName = "LevelCatalog",
        menuName = "Blossom Flow/Level Catalog")]
    public sealed class LevelCatalogAsset : ScriptableObject
    {
        [SerializeField] private AssetReferenceT<LevelDefinitionAsset>[] levels = System.Array.Empty<AssetReferenceT<LevelDefinitionAsset>>();

        public int LevelCount => levels?.Length ?? 0;

        public bool TryGetLevelReference(int levelNumber, out AssetReferenceT<LevelDefinitionAsset> levelReference)
        {
            levelReference = null;

            if (levels == null || levelNumber <= 0 || levelNumber > levels.Length)
            {
                return false;
            }

            levelReference = levels[levelNumber - 1];
            return levelReference != null && levelReference.RuntimeKeyIsValid();
        }

        public bool TryGetDefinition(int levelNumber, out LevelDefinition definition)
        {
            definition = null;

            if (!TryGetLevelReference(levelNumber, out var levelReference) || levelReference.Asset == null)
            {
                return false;
            }

            var levelAsset = (LevelDefinitionAsset)levelReference.Asset;
            definition = levelAsset.Definition;
            return definition != null;
        }

        private void OnValidate()
        {
            levels ??= System.Array.Empty<AssetReferenceT<LevelDefinitionAsset>>();
        }
    }
}
