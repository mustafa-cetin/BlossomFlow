using BlossomFlow.Gameplay.Domain.Levels;
using UnityEngine;

namespace BlossomFlow.Application.Levels
{
    [CreateAssetMenu(
        fileName = "LevelCatalog",
        menuName = "Blossom Flow/Level Catalog")]
    public sealed class LevelCatalogAsset : ScriptableObject
    {
        [SerializeField] private LevelDefinitionAsset[] levels = System.Array.Empty<LevelDefinitionAsset>();

        public int LevelCount => levels?.Length ?? 0;

        public bool TryGetDefinition(int levelNumber, out LevelDefinition definition)
        {
            definition = null;

            if (levels == null || levelNumber <= 0 || levelNumber > levels.Length)
            {
                return false;
            }

            var levelAsset = levels[levelNumber - 1];
            if (levelAsset == null)
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
