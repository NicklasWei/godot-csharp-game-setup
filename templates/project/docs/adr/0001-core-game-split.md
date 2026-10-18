# 0001: Engine-free Core library and a thin Godot layer

**Status:** Accepted (__DATE__)

## Context

Game rules (stats, generation, economy, AI decisions, saves) need fast, deterministic tests. Code that depends on
Godot types can only be tested inside a running engine, which is slow and awkward in CI, and rules spread across node
scripts run in scene-tree order, which tests can't pin down.

## Decision

- `src/__GAME__.Core` is a plain .NET class library with **no Godot reference**. It holds all game rules and uses
  `System.Numerics` for math. A test checks its assembly references.
- `__GAME__.csproj` (Godot.NET.Sdk) is the game layer: nodes, scenes, input, rendering, audio. It references Core;
  Core never references it. Core reaches the engine only through interfaces it defines (`ILog`, `IClock`, …).
- The Godot project stays at the repository root. Core, its tests, and the tools are excluded from the game project's
  compile glob (Godot.NET.Sdk otherwise compiles every `*.cs` under the root) and carry `.gdignore` files, so the
  editor doesn't scan them.
- Plain-string tables both layers need (input action names, localization keys, dev flags) live in Core, so
  file-based tests validate them without the engine.
- A stage's simulation is one Core session stepped in a fixed order; nodes are views.

## Consequences

- Most logic is unit-tested in milliseconds, and the game layer stays thin.
- Vector types are converted at the boundary (`game/common/NumericsInterop.cs`).
- Rules must not creep into node scripts; reviews enforce it, and the Core coverage gate makes it visible.
