using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using BlossomFlow.Gameplay.Domain.Boosters;
using BlossomFlow.Gameplay.Domain.Levels;
using Resolution = Reflex.Enums.Resolution;

public class GameInstaller : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.RegisterType(
            typeof(LevelSessionFactory),
            new[] { typeof(ILevelSessionFactory) },
            Lifetime.Scoped,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(SystemRandomNumberGenerator),
            new[] { typeof(IRandomNumberGenerator) },
            Lifetime.Scoped,
            Resolution.Lazy);

        builder.RegisterType(
            typeof(ShuffleBoosterService),
            new[] { typeof(IShuffleBoosterService) },
            Lifetime.Scoped,
            Resolution.Lazy);
    }
}
