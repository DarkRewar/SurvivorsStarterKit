# Contributing to Survivors Starter Kit

Thank you for your interest in contributing! This document outlines the rules and guidelines to follow when submitting a pull request. Contributions that do not meet these requirements will not be merged.

---

## Language

All contributions must be written in **English**: pull request titles, descriptions, code comments, variable names, method names, and documentation.

---

## Scope of Contributions

Pull requests must fall into one of the following categories:

- **Bug fixes** — correcting incorrect or broken behaviour in existing code.
- **Improvements** — making existing code, documentation, or architecture simpler, clearer, or more performant.
- **New features** — only accepted if they improve the **modularity** of the kit for forked projects (i.e. they make it easier for developers to extend or replace systems without modifying core code).

Pull requests that add game-specific content (new levels, art, story, etc.), opinionated gameplay changes, or features that benefit only one specific use case will not be merged.

---

## Language & Engine Rules

- All scripts must be written in **C#**. GDScript (`.gd` files) is not accepted.
- The project targets **Godot 4.6 (C# / .NET 8.0)** — do not upgrade engine version or target framework without prior discussion.
- Do not introduce new GDExtensions or third-party addons without opening a discussion first.

---

## Code Style

Follow the conventions already established in the codebase.

### Naming

| Element | Convention                     | Example                       |
|---|--------------------------------|-------------------------------|
| Classes | PascalCase                     | `EnemyManager`                |
| Public methods | PascalCase                     | `ApplyUpgrade()`              |
| Godot lifecycle overrides | Underscore prefix + PascalCase | `_Ready()`, `_Process()`      |
| Private / internal methods | PascalCase                     | `SpawnNextWave()`             |
| Private fields | `_camelCase`                   | `_gameManager`, `_lifepoints` |
| Public properties | PascalCase                     | `TotalDamages`                |
| Constants | PascalCase                     | `MaxLifepoints`               |
| Enums and enum values | PascalCase                     | `PowerupType.Damage`          |

### Classes

- One class per file; the filename must match the class name exactly.
- All Godot node scripts must use `public partial class`.
- Keep classes focused — a class should do one thing. If a class grows beyond its responsibility, split it.

### Properties and Fields

- Use `[Export]` for any value that should be configurable in the Godot editor.
- Prefer auto-properties with private setters: `public int Value { get; private set; }`.
- Use computed properties instead of methods when the result is a simple derivation: `public uint TotalDamages => Damages + _damagesBonus;`.
- Do not expose raw private fields publicly; use properties.

### Methods

- Methods must be **short and explicit**. If a method does more than one thing, split it.
- Method names must clearly describe what they do — avoid vague names like `Update()`, `Handle()`, or `Do()`.
- Prefer early returns over deeply nested conditionals.

### Enums

- Use enums for all categorical or type data (e.g. `EnemyClass`, `PowerupType`).
- Do not use magic strings or magic numbers.

### Signals and Events

- Use Godot `[Signal]` delegates when the event is consumed by the scene tree.
- Use C# `event Action<>` for code-to-code communication that does not cross scene boundaries.
- Do not mix both patterns for the same event.

---

## Architecture Rules

### Managers

- Collections of game entities (enemies, projectiles, etc.) must be managed by a dedicated `*Manager` class.
- Do not put spawning, pooling, or lifecycle logic directly in individual entity scripts.

### Interfaces

- Shared behaviour that multiple unrelated classes implement must be defined as an interface (see `IUpgradable`).
- Add new interfaces rather than duplicating logic across classes.

### Resources

- Game data that needs to be authored in the editor (powerups, enemy stats, etc.) must be defined as a Godot `Resource` subclass and serialised as `.tres` files.
- Static lists of resource paths belong in `PowerupPaths.cs` or an equivalent dedicated file — not scattered across scripts.

### GameManager

- `GameManager` is the central autoloaded singleton. Do not create additional autoloaded singletons.
- Cross-system communication should go through `GameManager` or events — avoid direct references between unrelated systems.

### Scenes and Prefabs

- Reusable game objects (enemies, projectiles, UI panels) must be defined as `.tscn` prefab scenes.
- Do not instantiate complex objects purely in code when a scene file would be more appropriate.

---

## Performance

This project targets 200+ simultaneous enemies. Any contribution that introduces significant per-frame overhead for large numbers of entities must include a justification and ideally a benchmark. In particular:

- Avoid allocations inside `_Process` or `_PhysicsProcess`.
- Prefer `_PhysicsProcess` for movement and collision logic.
- Keep Jolt physics layer usage intentional — do not add unnecessary collision shapes.

---

## Submitting a Pull Request

1. **Open an issue first** for any non-trivial change (new feature, architectural change, large refactor) so it can be discussed before you invest time writing code.
2. Keep pull requests **small and focused** — one logical change per PR.
3. Write a clear PR description explaining:
   - What the change does.
   - Why it is needed or useful.
   - How it improves modularity (for new features).
4. Make sure the project **opens and runs in Godot 4.6** without errors before submitting.
5. Do not include unrelated changes, formatting-only commits, or auto-generated files.

---

## Questions

If you are unsure whether a contribution fits the project's goals, open a GitHub issue and ask before writing code.
