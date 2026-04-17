# AGENTS.md

Guidance for agents and contributors working in this Unity project. The codebase uses the Reflex dependency inversion framework in `Assets/Reflex`, and gameplay/application code should follow Reflex conventions instead of creating service dependencies directly. Never compile. Don't create .meta files.

## Dependency Inversion Rules

- Treat Reflex as the composition root for systems, services, managers, repositories, factories, and other long-lived dependencies.
- Register dependencies from `IInstaller` implementations, such as `Assets/Scripts/Installers/RootInstaller.cs`.
- Prefer depending on abstractions (`IInputService`, `IInventoryRepository`, etc.) over concrete implementations when a type crosses feature boundaries.
- Do not use service locators, global mutable singletons, or ad hoc `new` calls for dependencies that should be controlled by the container.
- Keep Unity object lookup APIs (`FindObjectOfType`, `GameObject.Find`, scene-wide tag searches) out of business/gameplay logic unless the object is truly scene-local presentation state.
- Keep constructors and injected methods lightweight. Avoid doing heavy Unity work during injection itself.

## EventBus System

- The shared event infrastructure lives under `Assets/Scripts/Application/Events`.
- `EventBus` is the Reflex-owned implementation and is registered from `Assets/Scripts/Installers/RootInstaller.cs`.
- Depend on the smallest event interface needed:
  - Use `IEventPublisher` for classes that only publish events.
  - Use `IEventSubscriber` for classes that only subscribe to events.
  - Use `IEventBus` only when a class genuinely needs both publishing and subscribing.
- Define events as small typed message structs or classes near the feature that owns their meaning.
- Prefer feature-owned typed events when crossing feature boundaries. Keep the message types close to the feature that owns their meaning, such as level resolution events under gameplay presentation.
- Do not call `new EventBus()`, store it in a static field, or resolve it manually from gameplay code. Request it through constructor injection or `[Inject]` method injection.
- Subscriptions must be disposed or unsubscribed when the consumer stops listening. For `MonoBehaviour` consumers, subscribe in `OnEnable` or after injection and unsubscribe in `OnDisable` or `OnDestroy` as appropriate.
- Keep EventBus handlers lightweight. Publish intent/state-change events, then let subscribers coordinate their own cohesive work.
- Do not use the EventBus for direct request/response queries, persistent state storage, or replacing clear domain method calls inside the same cohesive feature.

## SOLID, Loose Coupling, and High Cohesion

- Follow SOLID across all gameplay and application code.
- Single Responsibility Principle (SRP): keep each class focused on one reason to change. Split input, domain logic, persistence, and presentation into separate types.
- Open/Closed Principle (OCP): extend behavior with new implementations, strategies, or handlers instead of modifying stable core flow classes.
- Liskov Substitution Principle (LSP): derived types and interface implementations must preserve expected behavior and not weaken invariants.
- Interface Segregation Principle (ISP): prefer small, focused interfaces over broad "god" interfaces.
- Dependency Inversion Principle (DIP): high-level policies depend on abstractions, while low-level details are bound in installers.
- Keep code loosely coupled: pass dependencies through constructor or `[Inject]` method injection, avoid hidden runtime lookups, and avoid direct cross-feature concrete type references.
- Keep code highly cohesive: organize classes by feature responsibility, keep methods narrow in purpose, and colocate related behavior/state while extracting unrelated concerns.
- Before adding a new class, verify whether it belongs to an existing feature boundary; if not, define a clear interface and register it in the appropriate installer.

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

## Blossom Flow Project Notes

- Use `turkishgamedesign.md` as the source design document for the current game concept.
- Keep `PROGRESS.md` updated as features move from design into implementation.
- The design draft mentions Zenject or VContainer as possible dependency injection options, but this Unity project is standardized on Reflex. Use Reflex only.
- Treat the first playable milestone as a deterministic core-loop prototype before adding lives, ads, IAP, boosters, Addressables, or late-game progression systems.
