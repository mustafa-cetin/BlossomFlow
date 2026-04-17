using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
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
    }
}
