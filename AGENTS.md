# AGENTS.md

Guidance for agents and contributors working in this Unity project. The codebase uses the Reflex dependency inversion framework in `Assets/Reflex`, and gameplay/application code should follow Reflex conventions instead of creating service dependencies directly.

## Dependency Inversion Rules

- Treat Reflex as the composition root for systems, services, managers, repositories, factories, and other long-lived dependencies.
- Register dependencies from `IInstaller` implementations, such as `Assets/Scripts/Installers/RootInstaller.cs`.
- Prefer depending on abstractions (`IInputService`, `IInventoryRepository`, etc.) over concrete implementations when a type crosses feature boundaries.
- Do not use service locators, global mutable singletons, or ad hoc `new` calls for dependencies that should be controlled by the container.
- Keep Unity object lookup APIs (`FindObjectOfType`, `GameObject.Find`, scene-wide tag searches) out of business/gameplay logic unless the object is truly scene-local presentation state.
- Keep constructors and injected methods lightweight. Avoid doing heavy Unity work during injection itself.

## Reflex Scopes

- Root-scope dependencies are shared by all injected scenes. Register project-wide services in root installers.
- Scene-scope dependencies belong to one scene. Add a single `ContainerScope` scene object when a scene needs injection.
- Do not create multiple scene scopes in the same scene.
- Scene scopes inherit from the root container by default.
- Only override a scene container parent through `ContainerScope.OnSceneContainerBuilding` when an additive scene hierarchy explicitly needs it.
- Do not rely on injected dependencies from `OnDestroy`; Unity does not call destruction deterministically in all cases.

## Registration Conventions

Use `ContainerBuilder` in installers:

```csharp
using Reflex.Core;
using Reflex.Enums;
using UnityEngine;

public sealed class RootInstaller : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.RegisterType(typeof(GameClock), new[] { typeof(IGameClock) }, Lifetime.Singleton, Resolution.Lazy);
        builder.RegisterFactory(container => new SaveService(), new[] { typeof(ISaveService) }, Lifetime.Singleton, Resolution.Lazy);
        builder.RegisterValue(new BuildInfo(Application.version), new[] { typeof(BuildInfo) });
    }
}
```

- Use `RegisterType` when Reflex should construct the object and inject its dependencies.
- Use `RegisterValue` only for objects already created outside the container. Values are always singleton-like.
- Use `RegisterFactory` when creation requires runtime data or a custom factory function.
- Choose lifetime deliberately:
  - `Singleton`: one shared instance.
  - `Scoped`: one instance per container.
  - `Transient`: a new instance per resolve.
- Choose resolution deliberately:
  - `Lazy`: create on first resolve.
  - `Eager`: create during container build for startup-critical systems or early validation.
- If multiple implementations share a contract, inject `IEnumerable<T>` for all of them or create typed wrapper contracts. Reflex does not support ID/name-based selective injection.

## Injection Conventions

- Use constructor injection for plain C# classes created by Reflex.
- Use `[Inject]` field, property, or method injection for `MonoBehaviour` components.
- Keep injected fields private whenever possible.
- Prefer a single `[Inject]` method on `MonoBehaviour` when initialization needs several dependencies.
- Do not manually call constructors for classes intended to be built by Reflex.
- If a class has multiple constructors and Reflex must construct it, mark the intended constructor with `[ReflexConstructor]`.

Example:

```csharp
using Reflex.Attributes;
using UnityEngine;

public sealed class PlayerPresenter : MonoBehaviour
{
    private IPlayerModel _model;
    private IAudioService _audio;

    [Inject]
    private void Construct(IPlayerModel model, IAudioService audio)
    {
        _model = model;
        _audio = audio;
    }
}
```

## Runtime-Created Objects

Objects instantiated after scene injection are not injected automatically.

- For runtime prefabs, attach `GameObjectSelfInjector` when inspector-driven injection is enough.
- For custom spawn pipelines, inject manually with `GameObjectInjector`.
- When resolving from a scene, use `gameObject.scene.GetSceneContainer()` only at integration boundaries such as factories, spawn systems, or adapters.
- Avoid calling `Resolve<T>` from arbitrary gameplay code. Prefer injecting the dependency into the object that needs it.

## Source Generation

- If a type uses `[Inject]` members and should benefit from Reflex source generation, add `[SourceGeneratorInjectable]`.
- Source-generated injectable types must be `public partial`.
- Containing/nested types must also be `public partial`.

Example:

```csharp
using Reflex.Attributes;
using UnityEngine;

[SourceGeneratorInjectable]
public partial class EnemyView : MonoBehaviour
{
    [Inject] private IEnemyFactory _enemyFactory;
}
```

## Unity Project Practices

- Keep installers small and grouped by responsibility. Add feature installers instead of growing one large installer when registrations become unrelated.
- Place app-wide installers under `Assets/Scripts/Installers` unless an existing feature folder has a clearer local pattern.
- Keep `Assets/Reflex` framework code vendor-like. Avoid modifying it unless the task is specifically about the framework itself.
- Do not add Zenject, VContainer, or another DI framework alongside Reflex.
- When introducing new services, add the interface, implementation, and installer registration in the same change when feasible.
- When writing tests for DI-bound classes, instantiate plain classes directly with fake dependencies or build a small `ContainerBuilder` for integration-style tests.

## Checklist Before Finishing DI Changes

- The dependency is registered in an appropriate root or scene installer.
- Consumers receive dependencies through constructor or `[Inject]`, not through global lookup.
- Runtime-created prefabs have a manual injection path.
- Lifetimes match ownership and disposal expectations.
- Scenes that require injection have exactly one `ContainerScope`.
- New code compiles without requiring changes inside `Assets/Reflex`.

## Blossom Flow Project Notes

- Use `turkishgamedesign.md` as the source design document for the current game concept.
- Keep `PROGRESS.md` updated as features move from design into implementation.
- The design draft mentions Zenject or VContainer as possible dependency injection options, but this Unity project is standardized on Reflex. Use Reflex only.
- Treat the first playable milestone as a deterministic core-loop prototype before adding lives, ads, IAP, boosters, Addressables, or late-game progression systems.
