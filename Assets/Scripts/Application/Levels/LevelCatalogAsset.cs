using UnityEngine;
using BlossomFlow.Gameplay.Domain.Levels;

namespace BlossomFlow.Application.Levels
{
    [CreateAssetMenu(
        fileName = "LevelCatalog",
        menuName = "Blossom Flow/Level Catalog")]
    public sealed class LevelCatalogAsset : ScriptableObject
    {
        [SerializeField] private LevelDefinitionAsset[] levels = System.Array.Empty<LevelDefinitionAsset>();

        public int LevelCount => levels?.Length ?? 0;

        public bool TryGetLevelAsset(int levelNumber, out LevelDefinitionAsset levelAsset)
        {
            levelAsset = null;

            if (levels == null || levelNumber <= 0 || levelNumber > levels.Length)
            {
                return false;
            }

            levelAsset = levels[levelNumber - 1];
            return levelAsset != null;
        }

        public bool TryGetDefinition(int levelNumber, out LevelDefinition definition)
        {
            definition = null;

            if (!TryGetLevelAsset(levelNumber, out var levelAsset))
            {
                return false;
            }

            definition = levelAsset.Definition;
            return definition != null;
        }

        private void OnValidate()
        {
            levels ??= System.Array.Empty<LevelDefinitionAsset>();
        }
    }
}
