# __TITLE__ — Coding Standards

> **Status:** Binding. [CLAUDE.md](../CLAUDE.md) summarizes the rules; this document is the full reference.
> If a rule blocks you, change it here deliberately (with an ADR if it's architectural, §3.11). Don't quietly work around it.
> Related: [GDD](GDD.md) · [Art Direction](ART_DIRECTION.md) · [Audio Direction](AUDIO.md) · [Implementation Plan](IMPLEMENTATION_PLAN.md)

Items marked **(verify)** depend on tool or engine behavior that hasn't been confirmed yet. Decisions are recorded in [docs/adr/](adr/README.md).

---

## 1. Principles

1. **Logic lives in plain C#; Godot renders it.** Game rules go in the engine-free `__GAME__.Core` library, where they're fast to test and deterministic. Godot nodes are thin adapters.
2. **Tests are part of the change.** Untested Core logic isn't done.
3. **Tools enforce the rules.** Formatting, analyzers, docs, banned APIs, and project-file checks fail the build or the tests. Review covers only what tools can't.
4. **Make it deterministic.** Anything driven by a seed reproduces exactly from that seed.
5. **Gameplay frames allocate nothing.** Steady-state gameplay allocates zero managed memory per frame.
6. **Keep things explicit.** Explicit dependencies, lifetimes, and data. No hidden globals.
7. **Use the lexicon.** Names come from [GDD §3](GDD.md#3-lexicon), in code, keys, UI, and docs.
8. **The docs can't drift.** Whatever can be generated from code (schemas, the wiki) is generated and checked in CI; what can't is updated in the same change.

---

## 2. Repository layout

The Godot project root is the repository root.

```
project.godot              Godot project
export_presets.cfg         Windows and Linux export presets, with the pack's filters (ADR 0011)
__GAME__.csproj            Game assembly (Godot.NET.Sdk); references Core
__GAME__.sln               All C# projects
global.json                Pinned .NET SDK (and the Microsoft.Testing.Platform opt-in)
Directory.Build.props      Shared build settings and analyzers (applies to ALL projects)
Directory.Packages.props   Central package versions
.editorconfig              Code style and analyzer severities
stylecop.json              StyleCop settings
BannedSymbols.Core.txt     Banned APIs for Core (§12.3)
BannedSymbols.Game.txt     Banned APIs for the game assembly
.config/dotnet-tools.json  Local tools (CSharpier, DocFX)
.csharpierrc.yaml          Formatter config
.githooks/                 pre-commit hook (§12.5) and Git LFS stand-in hooks
.claude/launch.json        Preview servers for Claude (the wiki)

src/__GAME__.Core/         Pure C# game logic. NO Godot references. Has .gdignore.
tests/__GAME__.Core.Tests/ xUnit v3 tests mirroring src/__GAME__.Core/, plus file-based checks of project.godot,
                           scenes, PO catalogs, and content. Has .gdignore.
tests/godot/               Godot-runtime tests: in-repo runner, [GodotTest] (ADR 0006). Visible to Godot, excluded
                           from exports.

game/                      Godot layer, grouped by feature (snake_case folders, Godot convention)
  app/ common/ controls/ l10n/ ui/ ...
shaders/                   *.gdshader, *.gdshaderinc, materials/ (shared materials)
content/                   JSON definitions loaded by Core, one folder per kind (ADR 0007)
  schemas/                 Generated JSON Schemas for editor validation
localization/              en.po (source of truth) plus one PO file per locale (ADR 0005)
assets/                    Imported assets: models/, audio/, fonts/, icons/
art-src/                   Source art (.blend, project files). .gdignore + Git LFS.
dev/                       Look-dev and sandbox scenes. Excluded from exports.
tools/                     Scripts and dev tools. Has .gdignore.
  __GAME__.Wiki/           Developer wiki generator (ADR 0008)
  wiki/                    DocFX site config and build scripts; generated/ and _site/ are gitignored
docs/                      GDD, art direction, audio direction, standards, plan, adr/. Has .gdignore.
```

**Rules:**
- `__GAME__.csproj` excludes `src/**`, `tests/__GAME__.Core.Tests/**`, `tools/**`, and the other non-game folders with `<Compile Remove>`, because Godot.NET.Sdk globs every `*.cs` under the root. It references Core with a `<ProjectReference>`.
- Every non-Godot folder (`src/__GAME__.Core/`, `tests/__GAME__.Core.Tests/`, `art-src/`, `docs/`, `tools/`) has a **`.gdignore`**, so the editor doesn't scan it or write `.uid` files into it. Don't put one on `tests/` as a whole: `tests/godot/` must stay visible to Godot.
- **Folder naming:** in `game/`, `shaders/`, `assets/`, `content/`, and `dev/`, folders and non-C# files use **snake_case** (`title_screen.tscn`). C# files use **PascalCase matching the type name**. Folders inside `src/`, `tests/__GAME__.Core.Tests/`, and `tools/` use PascalCase, following .NET.
- **Namespaces:**
  - Core: `__GAME__.Core.<Folder>`, enforced by IDE0130.
  - Game: `__GAME__.Game.<PascalCaseFeature>`, e.g. `game/ui/title/` → `__GAME__.Game.Ui.Title`. IDE0130 is off for `game/**` because the folders are snake_case; follow the mapping by hand.
  - **Never name a feature folder after a Godot class** (`input`, `os`, `time`, `engine`): the namespace would shadow the class everywhere below it. Input lives in `game/controls/`.
  - Core tests: `__GAME__.Core.Tests.<Folder>`. Runtime tests: `__GAME__.RuntimeTests.<Folder>`.
- **Ask before** adding an autoload, a top-level folder, a project, a NuGet package, or a Godot addon. Each one gets an ADR.

---

## 3. Architecture

### 3.1 Layers and dependency rules

```
┌──────────────────────────────────────────────┐
│ Game (__GAME__.csproj, Godot.NET.Sdk)        │  Nodes, scenes, input, rendering,
│   game/** → may use Godot + Core             │  audio, camera, adapters
└───────────────┬──────────────────────────────┘
                │ depends on (one direction only)
┌───────────────▼──────────────────────────────┐
│ Core (__GAME__.Core.csproj)                  │  Rules, simulation state, RNG,
│   src/__GAME__.Core/** → BCL only            │  content, saves, collision math
└──────────────────────────────────────────────┘
```

- **Core never references Godot.** No `using Godot;`, no GodotSharp package. Vectors use `System.Numerics`. `CoreArchitectureTests` checks the assembly references.
- **The game layer converts at the boundary** with `game/common/NumericsInterop.cs` (`ToGodot()` / `ToNumerics()`).
- **Dependencies point inward only.** Core reaches the engine only through interfaces Core defines and the game layer implements: `IClock`, `ILog`, later `IFileStore`.
- **No mutable static state** anywhere. Static `readonly` immutable data (tables, `StringName` caches) is fine. `CoreArchitectureTests` fails on a non-readonly static field in Core.
- Plain-string tables both layers need (input action names, localization keys) live in Core, so file-based tests check them without the engine.

### 3.2 Core: what belongs there

Everything that can be expressed without the engine: rules and formulas, simulation state and its tick, generators, content loading and validation, saves and migrations, economy, AI decisions, collision math, state machines, RNG, localization keys, input action names, dev-flag parsing, debug console commands. Organize it by domain folder (`Combat/`, `World/`, `Items/`, …), each with a mirrored test folder.

### 3.3 Game layer: what belongs there

- **Nodes are views and controllers.** They read input, call Core, and present Core state through transforms, visuals, audio, and UI.
- **A node script holds no game rules.** An `if` about damage, cost, chance, or progression in a node belongs in Core.
- **Feature folders are self-contained.** A feature's scenes, scripts, and resources live together; shared things move to `game/common/` or `shaders/`.
- **Call down, signal up.** Parents call methods on children. Children raise C# events (§5.5). A node never reaches up or sideways with `GetParent()` or absolute paths.

### 3.4 Composition root and dependency injection

- **The composition root is `game/app/Main.cs`**, the main scene's root. It builds the services (log, clock, locale, content, later save, settings, audio, flow) and owns their lifetimes.
- **Explicit initialization:** a scene that needs dependencies exposes `Initialize(...)`, and its parent calls it **before** `AddChild`. `_Ready` checks it ran (`NodeGuard.RequireInitialized`).
- **At most one autoload**, and only for a truly scene-independent lifetime. It is never a service locator.
- No DI container. When a parameter list passes about five, bundle it in a record (`AppServices`).

### 3.5 Simulation in Core; nodes are views

- A gameplay stage's whole simulation is one Core object (a *session*) that steps every system **in one fixed, documented order** per physics tick and exposes read-only state plus a per-tick buffer of events. Update order spread across node `_PhysicsProcess` methods depends on scene-tree order, which can't be unit-tested.
- One gameplay node per stage reads input, calls `Step`, and pushes state to child views. Views hold no rules and never talk to each other. Presentation (sound, VFX) reacts to the events after the step and never mutates Core state.
- Core collections expose stable slot indices or packed spans, so views map them to pooled nodes or one `MultiMeshInstance3D` without allocating.
- Sibling stage kinds share an abstract session base for everything common, rather than forking.
- Whole stages run in unit tests: identical inputs give identical results, and a test asserts 0 B allocated per step under full load.

### 3.6 Randomness and determinism

- Gameplay randomness comes only from Core's `RngStream` (xoshiro256\*\*, seeded through SplitMix64, ADR 0009). `System.Random` and `GD.Rand*` are banned (§12.3).
- **Named streams:** derive one stream per independent consumer (world generation, loot, combat, AI) from the session seed, so one system's draws never shift another's. `RngStream.Derive(key)` gives a child stream per level, room, or encounter without advancing the parent.
- Player actions must never shift the streams that build the world or roll rewards. Tests cover it.
- **Cosmetic randomness** (visual-only) uses its own stream seeded from OS entropy, and must never affect gameplay.
- **Never change** the generator, the seed expansion, the stream salts, or how a draw method uses bits: shared seeds and saves depend on them. Golden tests pin the outputs.
- Time comes from `IClock`; wall-clock and tick-count APIs are banned in Core.

### 3.7 Content (data-driven definitions)

- **Format:** JSON in `content/<kind>/<name>.json`, parsed by Core with **System.Text.Json source generation** (`ContentJsonContext`): reflection-free and trim-safe. Comments and trailing commas are allowed. **Unknown properties are errors**, so a typo never passes silently. A test checks every kind uses these options.
- **Kinds** are registered once in `ContentKinds.All` (folder, ID prefix, document type, validator, localization keys). The loader, the schema tests, the localization tests, and the wiki all work from that list.
- **Documents and definitions:** a document is the raw JSON shape (nullable properties, derives from `ContentDocument`); the kind's validator turns it into an immutable definition record that game code uses. Enum-like values are camelCase strings in JSON, mapped by the validator; never ordinals.
- **IDs:** `<prefix>.<snake_case_name>`, where the name is the file name (the loader enforces it). IDs are **permanent**: saves and other content reference them. Never rename or reuse one; deprecate it instead. Content names of enum values are as permanent as IDs.
- **Loading:** `ContentLoader` builds one `ContentDatabase` and collects every problem with its file path. The game loads it once at boot through Godot's file API (`ContentFiles`, which works in exports) and passes it down. Broken files are logged and left out.
- **Tuning numbers** belong in content, not in `[Export]` fields. Numbers still in code live in public `…Settings` records with named static instances, which the wiki lists. Docs refer to them by identifier, never by value (§6.2).
- **Validation** runs in Core and in tests over the real `content/` folder: unique IDs, resolving cross-references, sane ranges, required localization keys.
- **Schemas:** `content/schemas/<kind>.schema.json` are **generated** from the document types (`JsonSchemaExporter`) and give editors validation and autocomplete. Every content file starts with `"$schema": "../schemas/<kind>.schema.json"`. A test fails when a schema is stale; regenerate with `UPDATE_CONTENT_SCHEMAS=1 dotnet test --project tests/__GAME__.Core.Tests`. Never edit one by hand.
- **Asset references** in content are `res://` paths; a runtime test checks they exist and load.
- **Export:** the game reads `res://content` through Godot's file API, so the content must be in the pack. Godot 4 counts `.json` files as resources, and the export presets also include `content/*` by name and exclude `content/schemas/*` (ADR 0011).

### 3.8 Save data

- Files under `user://`: settings, persistent progress, and an in-progress session if the game suspends one.
- **Versioned envelope** `{ "version": N, "data": { … } }`. Migrations are classes `MigrationVnToVn+1`, applied in a chain. **Every migration has a fixture test.**
- **Atomic writes:** write `*.tmp`, then replace the target and keep the previous file as `*.bak`.
- **Corrupt files:** load the `.bak`. If that fails too, rename the corrupt file to `*.corrupt-<timestamp>` and start fresh with a visible warning. **Never silently discard progress.**
- Core uses `IFileStore`; the game layer implements it on `ProjectSettings.GlobalizePath("user://…")`.
- Persist string IDs and explicit values, never enum ordinals or reflection-driven shapes.

### 3.9 State machines and game flow

- When flow first needs one, Core gets one small, generic, hierarchical `StateMachine<TState, TTrigger>`, unit-tested once and reused everywhere (flow, AI, enemy phases). Don't grow a second.
- **Game flow is a Core state machine** (boot → title → play → results → …). The game layer's flow controller listens for state changes and presents them; it never decides transitions itself.
- **Decide early how transitions look.** Scene swaps with fades are cheap to build; a continuous world (no loading screens, content streamed in behind motion) needs streaming, origin rebasing, and local transforms from the start (§5.6). Changing from one to the other late is expensive, so it's an ADR in the planning phase.

### 3.10 Gameplay collision and physics

- When gameplay needs many hit tests per tick (hundreds of projectiles) or exact determinism, resolve hits in a **Core collision world** (primitive shapes, a grid broadphase, layer masks, swept tests), which is deterministic, allocation-free, and unit-tested. Godot's C# physics queries return `Godot.Collections` dictionaries, which allocate, and one `Area3D` per projectile doesn't scale.
- Otherwise Godot physics (Jolt in 3D) is fine, wrapped behind the stage's controller. Choose in a spike with numbers, and record the ADR.
- Cosmetic physics (debris, ragdolls) may always use Godot physics.

### 3.11 Architecture Decision Records

Any architectural choice goes in `docs/adr/NNNN-kebab-title.md` (Context / Decision / Consequences, under a page): layout, frameworks, renderer, collision, save format, new dependencies. Accepted ADRs are immutable: supersede them with a new ADR and edit only the old one's status line. Spikes end in an ADR with their measurements.

---

## 4. C# language standards

### 4.1 Project settings (all projects, via `Directory.Build.props`)

- `LangVersion` `latest`, `Nullable` `enable`, `ImplicitUsings` `enable`, `TreatWarningsAsErrors` `true`.
- `AnalysisLevel` `latest-recommended`, `EnforceCodeStyleInBuild` `true`, `GenerateDocumentationFile` `true`, `Deterministic` `true`, `Features` `strict`.

### 4.2 Naming

| Element | Convention | Example |
|---|---|---|
| Namespace, type, method, property, event, enum member, constant | PascalCase | `StatSheet`, `ApplyModifier` |
| Interface | `I` + PascalCase | `IMovementFrame` |
| Private/internal instance field | `_camelCase` | `_modifiers` |
| Private static readonly field | PascalCase | `PauseAction` |
| Parameter, local | camelCase | `baseValue` |
| Generic type parameter | `T` + PascalCase | `TState` |
| Async method | `…Async` suffix | `LoadProfileAsync` |
| Event | Past tense or `…ing`, no `On` prefix | `Destroyed`, `ItemCollected` |
| Event handler method | `On` + source + event | `OnPlayerDestroyed` |
| Test method | `Method_Scenario_Expected` | `Evaluate_FlatThenPercent_AppliesFlatFirst` |
| Boolean | Reads as a predicate | `IsGrounded`, `CanJump` |

Use lexicon terms ([GDD §3](GDD.md#3-lexicon)) for every game concept.

### 4.3 Types and members

- One top-level type per file, named after it. File-scoped namespaces; `using` directives outside the namespace.
- **Sealed by default.** Unseal only for a designed extension point, and document it.
- Explicit access modifiers, smallest visibility that works. Core-only types are `internal`, with `InternalsVisibleTo` for tests.
- **Immutable by default:** `record` / `record struct` / `readonly struct` for values, `init` for definitions, `IReadOnlyList<T>` / `IReadOnlyDictionary<,>` / `ReadOnlySpan<T>` in signatures.
- `readonly struct` for small hot-path values (≤ 16–24 bytes); pass large structs by `in`.
- `var` only when the type is obvious from the right-hand side.
- Expression bodies for one-liners only.
- Enums: explicit values when persisted, and **never persist ordinals**: persist string IDs.
- No regions. `#if TOOLS` / `#if DEBUG` only for editor and debug code.

### 4.4 Nullability

- **No `!` (null-forgiving) operator**, except `[Export] private T _x = null!;` fields validated in `_Ready` (`NodeGuard.RequireExport`).
- Validate public arguments with `ArgumentNullException.ThrowIfNull` and friends.
- Prefer `TryGet…(out T value)` over returning null for "not found".

### 4.5 Errors and exceptions

| Situation | Handling |
|---|---|
| Programmer error (bad argument, invalid state) | Throw. Fail fast. |
| Expected failure (can't afford, slot full) | `bool TryX(...)` or a small result record. **Never exceptions for control flow.** |
| Content errors | Collected per file with its path; logged at boot; caught by tests long before. |
| I/O and save errors | Handled at the boundary with recovery (§3.8), logged with context. |

- Never `catch (Exception)` without rethrowing, except at defined boundaries (save loading, top-level flow), and then always log.
- Suppressions use `[SuppressMessage(..., Justification = "...")]` at the narrowest scope. **Never a bare `#pragma warning disable`.**

### 4.6 Async and threading

- **Godot objects are main-thread only.** Marshal worker results back with `CallDeferred` or a main-thread queue.
- Core may do pure-data background work (preparing the next level); results are immutable or handed over once. No `Task.Run` in Core: callers decide where work runs.
- `async void` only for Godot event handlers, which catch and log. `await ToSignal(...)` is fine for one-shot sequencing in the game layer, never in Core.

### 4.7 Collections and LINQ

- LINQ is fine in setup, loading, tools, and tests, and **forbidden in per-frame paths** (§8).
- Prefer arrays, `List<T>` with capacity, and `Span<T>`. String-keyed dictionaries take `StringComparer.Ordinal`.
- Never `Godot.Collections.Array` / `Dictionary` outside Godot API boundaries: they marshal and allocate.

---

## 5. Godot C# standards

### 5.1 Scripts and scenes

- Godot script classes are `partial`, and the file name equals the class name.
- One script per scene root, which owns the scene's behavior. Child nodes get scripts only as reusable components.
- **Scenes are instantiable in isolation**: every scene loads and instantiates on its own (`SceneLoadTests`), and fails with a clear error if `Initialize` wasn't called.
- `PackedScene.Instantiate<T>()` with a typed root, then `Initialize`, then `AddChild`.
- Unique names (`%Name`) only within a scene, for its own children. Groups only for broad queries, with `StringName` constants.

### 5.2 Node references and exports

- Typed `[Export]` references over `GetNode("path")`; cache any lookups in `_Ready`.
- Exported references are `private` fields set to `null!` and validated in `_Ready`:

```csharp
[Export]
private Label _buildLabel = null!;
```

- Never `GetNode`, `FindChild`, or group queries in per-frame code.
- Exports are for wiring and purely visual parameters. Tunable gameplay numbers belong in content (§3.7).

### 5.3 StringName, NodePath, and Variant

- Every repeated `StringName` / `NodePath` (input actions, animations, shader parameters, groups, signals) is a `static readonly` field. Implicit `string` → `StringName` conversion allocates on every call.
- Avoid `Variant` in hot paths; use typed APIs.

### 5.4 Input

- Actions are defined in `project.godot`; their names are constants in Core (`InputActionNames`), wrapped as `static readonly StringName`s in `game/controls/InputActions.cs`.
- **Every action has both a keyboard/mouse and a controller binding.** A file-based test parses `project.godot` to enforce it, and checks the map matches `InputActionNames` exactly.
- Read gameplay input in `_PhysicsProcess` by polling (`Input.GetVector`, `Input.IsActionPressed`) and pass plain values into Core.
- **Don't override `_Input` / `_UnhandledInput` for continuous input** such as mouse motion: Godot allocates a managed wrapper for every event delivered to C#. Poll `Input` / `DisplayServer` instead. Overrides are fine on screens without a 0 B/frame budget (menus).
- Whatever shows the mouse cursor owns it, and hides it again when it closes.

### 5.5 Signals vs C# events

- C# events by default; `[Signal]` only for editor wiring, `await ToSignal`, or engine interop.
- Subscribe with named methods, not lambdas, and **unsubscribe in `_ExitTree`**. A lambda capturing `this` keeps freed nodes alive and leads to `ObjectDisposedException`.

### 5.6 Lifecycle and process

- **Gameplay runs in `_PhysicsProcess`** at a fixed tick (60 Hz), with **physics interpolation on**. `_Process` is for visuals only.
- After teleporting a node, call `ResetPhysicsInterpolation()`. An origin rebase (moving a whole stage root, camera included) is not a teleport and must not reset: a reset there drops one interpolated step and shows as a hitch. A camera that takes over in that tick shadows the current camera first, then takes over.
- **Views set local transforms** (`Transform`, `Position`), never `GlobalTransform`, so a stage root can be moved or rebased; screen projections go through the stage's transform.
- **Streaming:** prepare everything that needs no engine on a worker as Core data, then create engine objects a step at a time within a per-frame budget on the main thread.
- **Pause** uses `ProcessMode`, not `if (paused)` checks.
- `_Ready`: validate exports, cache references, subscribe. No heavy work. `_ExitTree`: unsubscribe, stop owned tweens and timers, release pooled objects.
- Disable idle processing with `SetProcess(false)` rather than early-returning every frame.

### 5.7 Object lifetime

- `QueueFree()`, never `Free()`, unless the node is out of the tree and unreferenced.
- `GodotObject.IsInstanceValid(obj)` before using a reference that may outlive its target.
- Pooled nodes (projectiles, enemies, VFX) are deactivated, never freed, during gameplay.
- Don't hold node references across scene changes; hold IDs or Core data.

### 5.8 Resources and materials

- Loaded `Resource`s are shared: **never mutate one at runtime.** Per-object variation uses `instance uniform`s; duplicate once at load only when there's no other way.
- Preload `PackedScene`s and materials at load time, not mid-gameplay.
- **Hold a resource loaded by path for as long as anything may load it again.** One held by nothing but a dropped C# wrapper is freed later by the wrapper's finalizer, and a load that takes it from the resource cache between the collection and the finalizer leaves its binding broken (`Handle is not initialized`, godot#112067, [ADR 0006](adr/0006-godot-runtime-tests-and-ci.md)). `Main` holds the game's scenes in `[Export]` fields for the session, and runtime tests load theirs through `GodotTestContext.Scene`.
- Custom `[GlobalClass]` resources are for editor-authored visual or scene data; gameplay definitions are content JSON.

### 5.9 Editor tool scripts

- C# `[Tool]` scripts and import scripts are allowed, wrapped in `#if TOOLS` when they use editor APIs, small, and side-effect free. A C# build error also breaks editor imports until fixed.

### 5.10 Scene and resource file hygiene

- When editing `.tscn` / `.tres` as text, reference resources by `path="res://..."` and **never invent `uid://` values**; Godot fills them in on the next save.
- **Commit** Godot's `*.uid` and `*.import` files. **Never commit** `.godot/`.
- After structural text edits, open and re-save the scene in the editor when possible so Godot normalizes it.

---

## 6. Documentation standards

### 6.1 XML documentation

**XML documentation is required on every type and member, including private methods** (CS1591 + SA1600 with `documentPrivateElements: true`). Private fields are exempt.

| Element | Requirement |
|---|---|
| Types | `<summary>`: what it is and its responsibility. `<remarks>` for invariants, threading, lifetime. |
| Methods | `<summary>`, `<param>` each, `<returns>`, `<exception>` each documented throw, `<remarks>` for non-obvious behavior ("allocation-free"). |
| Properties | `<summary>` starting "Gets …" / "Gets or sets …", **with units** (`m/s`, `seconds`). |
| Events | When raised, and on which thread. |
| Overrides, interface implementations, Godot lifecycle methods | `<inheritdoc/>` |
| Tests | Test classes need a `<summary>` naming the unit under test; test methods are exempt (the name is the doc). |

Document **intent, units, and invariants**, not the name restated. "Gets the speed." fails; "Gets the forward speed in m/s, clamped to [MinSpeed, MaxSpeed]." passes. These summaries are also the wiki's descriptions (§13).

### 6.2 Docs and values

When a change alters design, look, sound, architecture, or a standard, update the doc in the **same** change: the GDD, the art direction, the audio direction, this file, an ADR, and the implementation plan's checkboxes.

**Once something is implemented, the code is the source of truth for its values.** A tunable value, anything that might be adjusted for feel or balance (a speed, a cooldown, a cost, a spawn rate, a count), lives in one place: content or a `…Settings` record (§3.7). Changing it is then a one-line change that no doc contradicts.

- **Docs name the identifier, never the value.** A doc that refers to an implemented value names the setting or content field and keeps the reasoning; the number goes. Before implementation the GDD says "a dash recovers in 0.6 s *(tunable)*, short enough to chain into a parry"; after it, "a dash recovers quickly enough to chain into a parry (`DashSettings.Cooldown`)".
- **Values a doc needs to show are generated.** The wiki already renders every content definition and `…Settings` record (§13). Another view of them, such as a balance table, gets a page writer, never a hand-typed table.
- **State relations, not numbers.** When the design depends on how values relate (a dash recovers before the parry window closes), write the relation and enforce it with a content validation or a test.
- **XML docs too:** units, ranges, and invariants by name (`clamped to [MinSpeed, MaxSpeed]`), never the current default.
- **A change to an identifier includes the docs.** When you rename or remove a setting, content field, or constant, or change its units or meaning, search `docs/`, `CLAUDE.md`, and code comments for it and update every reference in the same change; the compiler doesn't see them. When you change a value, search the docs for the old value too.

A doc gives a precise value only in these cases:

- **Before implementation:** the GDD and the implementation plan give preliminary values, marked *(tunable)*, each with its reason. The change that implements one moves the value into code and replaces it in the docs with the identifier.
- **History:** an ADR or a spike result may give the values its decision was made around (the measurements, the budget chosen from them), as of its date. A value the decision doesn't depend on is described ("a short cooldown"), not given precisely.
- **Non-tunable values the section needs:** an external standard or a target the doc itself sets (a loudness target, a performance budget), an engine or platform limit, a contract or format (the ID pattern, a save version), or a constant whose change would be a redesign rather than a tuning pass.

---

## 7. Testing standards

### 7.1 Test pyramid

| Level | Where | Speed | What |
|---|---|---|---|
| Unit (Core) | `tests/__GAME__.Core.Tests` | ms | Every rule, generator, model, migration. The bulk. |
| Project files | `tests/__GAME__.Core.Tests` | ms | `project.godot` (input map, catalogs), scenes (keys), PO files, real content, schemas. |
| Godot runtime | `tests/godot` | s | Scenes load; screens in the live tree; Core and nodes together; 0 B/frame. |
| Manual and visual | look-dev, playtests, movie-writer captures | — | Feel, readability, performance on the minimum-spec hardware. |

### 7.2 Frameworks

- **Core:** xUnit v3 on Microsoft.Testing.Platform (opt-in in `global.json`), built-in `Assert`. **No FluentAssertions** (commercial license from v8).
- **Fakes over mocks:** hand-written (`RecordingLog`, `FakeClock`). No mocking library without an ADR.
- **Godot runtime:** the in-repo runner (ADR 0006). Tests are `public static` methods with `[GodotTest]` under `tests/godot/`, taking an optional `GodotTestContext`, returning `void` or `Task`, asserting with `GodotAssert`. A test also fails if Godot logs an engine, script, or shader error while it runs. `AllocationProbe` measures main-thread allocations from inside the frame loop.

### 7.3 Conventions

- Test folders mirror source folders; classes are `<TypeUnderTest>Tests`; methods `Method_Scenario_Expected`.
- Arrange / Act / Assert with comments; one behavior per test; `[Theory]` for tables of cases.
- Builders for big object graphs; independent, order-free tests; temp directories only.

### 7.4 What must be tested

- Every public Core API, with edge cases (zero, negative, max, empty).
- **Every bug fix gets a regression test** that fails before the fix.
- **Generators:** invariant tests across many seeds (1,000) and golden tests for a fixed seed.
- **Determinism:** same seed and inputs, same result; player actions don't shift world or reward streams.
- **Saves:** round-trip per model; a fixture test per migration.
- **Content:** validation over the real `content/` folder; schemas up to date.
- **Localization:** key coverage, no orphans, key-set parity, fuzzy flags, placeholder parity, registered catalogs.
- **Hot paths:** allocation tests. Warm up, then assert `Allocations.Measure(...)` is 0. It takes the lowest of up to three runs, because coverage instrumentation and JIT tiering occasionally allocate once on a first-time path; a real per-call allocation is never zero.

### 7.5 Determinism in tests

- Inject `IClock` and seeded `RngStream`s. **No `Thread.Sleep`, no wall-clock time.**
- Floating-point asserts use explicit tolerances unless exact arithmetic is guaranteed.

### 7.6 Godot runtime tests

These exist from day one and stay green:
- **Scene smoke:** every `.tscn` under `game/` and `dev/` loads, instantiates, and frees without orphans.
- **Main boots** to its first screen with no engine errors.
- **Allocation budget:** each gameplay screen or stage runs at **0 B/frame** under `AllocationProbe` (the template checks the title screen; every new stage adds its own).
- **Content through `res://`:** the real content loads through Godot's file API.
- Input-map and scene-key checks are file-based Core tests (faster, no engine).
- **Scenes come from the context:** tests load scenes with `context.Scene` or `context.Instantiate`, never `ResourceLoader`, so the runner holds them for the run. After each test the runner finalizes the C# wrappers the test left, so none is finalized during a later test; `--gc-stress` holds them unfinalized through the next test instead, to catch a test or view that leaves the race in [ADR 0006](adr/0006-godot-runtime-tests-and-ci.md) open.

### 7.7 Coverage

- **Core line coverage ≥ 85%**, enforced in CI by `coverlet.MTP` (`--coverlet-threshold 85 --coverlet-threshold-type line`). A floor, not a goal: no assertion-free tests.

---

## 8. Performance

Budgets are in [Art Direction §14](ART_DIRECTION.md#14-performance-budgets). The code rules:

- **Zero steady-state managed allocations per gameplay frame.** The debug overlay shows allocated bytes per frame; runtime tests assert it. Any non-zero steady value is a bug.
- **Forbidden in `_Process`, `_PhysicsProcess`, and anything they call every frame:**
  - LINQ, capturing lambdas, `new` on reference types, boxing.
  - String concatenation, interpolation, or formatting (pre-build texts at load; debug overlays behind `#if DEBUG`).
  - Implicit `string` → `StringName` / `NodePath` conversion.
  - `GetNode` / `FindChild` / group queries; `Godot.Collections`; physics queries returning dictionaries.
  - Unguarded logging (check `IsEnabled` first).
  - `_Input` overrides for continuous input (§5.4); C# `CompositorEffect` callbacks (they marshal managed wrappers every frame).
- **Infrequent per-frame work counts too:** formatting a counter string "only" on each kill still allocates during gameplay. Pre-build texts.
- **Pools:** `ObjectPool<T>` (Core) and `NodePool<T>` (game), added when first needed and pre-warmed at load. Many instances: `MultiMeshInstance3D`.
- **Measure before optimizing,** and leave a comment with the measurement when you do. Run the benchmark at every phase exit and compare against the minimum-spec tripwire.

---

## 9. Logging and diagnostics

- Core logs through `ILog` with a category. `GodotLog` is the only class allowed to call `GD.Print*` / `GD.Push*` (banned elsewhere, §12.3).
- Guard hot-path logging with `if (log.IsEnabled(LogLevel.Debug))`.
- **Dev flags** (`DevFlags`, parsed in Core, listed in the wiki) are passed after `--`. `--autopilot` drives the game with scripted input, so CI and captures exercise gameplay without a person; add boot-straight-into-gameplay and seed-replay flags as soon as there's gameplay.
- **Visual checks without a person:** render frames with Godot's movie writer and look at the PNGs:
  `"$GODOT_BIN" --path . --resolution 1280x720 --write-movie <dir>/f.png --fixed-fps 60 --quit-after 600 -- --autopilot`.
  It opens a window, runs slower than real time, and also writes `f.wav`. Its dummy audio driver logs leaked `AudioStreamPlayback` errors at exit, which are harmless there; headless runs mute audio instead.
- Debug-only tooling (overlay, console) sits behind `#if DEBUG` or a debug flag and never ships enabled. Console commands run against Core state, so they're testable.

---

## 10. Localization in code

- **Locales:** __LOCALES__, as gettext **PO files** in `localization/` (ADR 0005). PO supports plural forms; Godot's CSV translations don't.
- **The `msgid` is a stable key**, not English text: lowercase, dot-separated, snake_case segments (`ui.menu.start`, `item.iron_sword.name`). `en.po` holds the English text and defines which keys exist.
- **Code** uses `L10nKeys` constants (Core). Content keys come from `L10nKeyBuilder` (`<id>.name`, `<id>.desc`) and each kind declares the keys it needs. **No string literals to `Tr()`.**
- **Scenes:** `Label` / `Button` text is the key, with auto-translate on. Labels whose text code sets get `auto_translate_mode = 2`.
- **Formatting:** indexed placeholders (`{0}`), formatted with the locale's `CultureInfo` (`LocaleService.Format`). Plurals use `TrN` with `.plural` keys.
- **New keys** go into `en.po` and into every other locale with an empty `msgstr` and `#, fuzzy`. **Never machine-translate silently.** A new locale is a new PO file with every key, registered in `project.godot`; the tests find it.
- **Layout:** the UI survives 35% text expansion and CJK line breaking (exports include the TextServer ICU data). Run with `-- --pseudoloc` to catch hardcoded strings and overflow.

---

## 11. Shaders and visuals

- **Colors come from palette roles** (`pal_*` global shader uniforms), never raw colors in shaders or scenes. Readability rules ([Art Direction §3](ART_DIRECTION.md#3-readability-rules)) beat style.
- Files are snake_case in `shaders/`; shared code goes in `.gdshaderinc`. Duplicating a shader function is a review failure.
- **Every shader starts with a header comment:** purpose, render mode rationale, every uniform with range and units, and the globals it reads.
- Uniforms are snake_case with hints; per-object variation uses `instance uniform`. No magic numbers: named `const`s.
- Every shader works on every supported renderer (the chosen one and its fallback) unless marked optional.
- **Look-dev coverage:** every shader and material appears in the look-dev scene (`dev/`); a file-based test enforces it once the scene exists. Also in the shader warm-up scene.
- **Effects scaling:** flashes, shake, and glitch-style effects scale with an accessibility intensity setting, and nothing flashes full-screen faster than 3 Hz.

---

## 12. Tooling

### 12.1 `Directory.Build.props`

Shared by every project (see the file). Package versions live in `Directory.Packages.props` (Central Package Management). The .NET SDK is pinned exactly in `global.json` with `rollForward: latestPatch`, and CI uses the **same exact SDK** (ADR 0002): a newer feature band brings new analyzers, which fail a warnings-as-errors build in CI but not locally. Bump both in one commit, with any new findings fixed.

### 12.2 Analyzers and severity policy

.NET analyzers (`latest-recommended`), **StyleCop.Analyzers**, **Roslynator.Analyzers**, and **BannedApiAnalyzers**, all errors. Key `.editorconfig` decisions:

| Rule | Setting | Why |
|---|---|---|
| `SA1101` | Off | No `this.` prefix |
| `SA1309` off, `SX1309` error | | Private instance fields are `_camelCase` |
| `SA1633` | Off | No file headers |
| `SA1600`, `CS1591` | Error | Docs required (relaxed for test methods) |
| `IDE0130` | Error in `src/**`, off in `game/**` | Namespace matches folder; game folders are snake_case |
| StyleCop layout rules that conflict with CSharpier | Off | CSharpier owns layout (list in `.editorconfig`, ADR 0004) |

Suppressions: `[SuppressMessage]` with a real justification at the narrowest scope, or `.editorconfig` for folder-wide policy. Every suppression is reviewed.

### 12.3 Banned APIs

`BannedSymbols.Core.txt`: `System.Random` → `RngStream`; `DateTime.Now` and friends, `Environment.TickCount` → `IClock`; `Console` → `ILog`; `Thread.Sleep` → nothing.
`BannedSymbols.Game.txt`: `GD.Print*` / `GD.Push*` → `ILog` (`GodotLog`); `GD.Rand*` and `System.Random` → `RngStream`; wall-clock time → `IClock`; `Console.Write*` per method (Godot's generated code references `Console`, so the type itself can't be banned); `Thread.Sleep`.
Each overload needs its own documentation ID; verify a new entry by making the build fail on it once.

### 12.4 Formatting

**CSharpier** (local tool, `printWidth: 120`) owns C# and MSBuild XML layout and sorts `using` directives: `dotnet csharpier format .` / `dotnet csharpier check .`. Never hand-format against it, and don't mix formatting into unrelated commits. Non-C# files follow `.editorconfig` (UTF-8, LF, final newline; tabs in shaders).

### 12.5 Git hooks

`tools/setup-hooks.ps1` / `.sh` set `core.hooksPath` to `.githooks` (and install Git LFS). The pre-commit hook runs the format check, the build, and the Core tests. Because `core.hooksPath` bypasses `.git/hooks`, `.githooks/` also carries the Git LFS hooks. Hooks are a convenience; CI is the authority.

### 12.6 CI pipeline

| Stage | Job | Gate |
|---|---|---|
| verify | format | `dotnet csharpier check .` |
| verify | build | `dotnet build __GAME__.sln -c Debug` and `dotnet build __GAME__.csproj -c ExportRelease`, zero warnings |
| test | core-tests | xUnit, Core line coverage ≥ 85%, JUnit and Cobertura reports |
| test | wiki | the generator with `--strict`, then DocFX with `--warningsAsErrors` |
| godot | godot-smoke | Downloads headless Godot .NET for Linux, imports, boots the main scene, runs the Godot-runtime tests (JUnit), and fails on any `ERROR` / `SCRIPT ERROR` / `USER ERROR` line |
| publish | pages | Publishes the wiki from the default branch |
| export | (later) | Exports the committed presets on tags (ADR 0011), with the export templates cached |

### 12.7 Task runner

Day-to-day commands are recipes in the root `justfile` ([just](https://github.com/casey/just), ADR 0010): `just` lists them, and `just ci` runs every gate in CI order. Recipes run in PowerShell on Windows and `sh` elsewhere, and read machine-specific settings (`GODOT_BIN`) from a git-ignored `.env` (template: `.env.example`; a Windows path goes in single quotes, since unquoted backslashes stop just from loading the file). Unix recipes run the shell scripts through `sh`, because a project scaffolded on Windows commits them without the executable bit. A new routine command gets a recipe with a one-line comment, which `just --list` shows. `just export` (or `export-windows`, `export-linux`) writes release builds into the git-ignored `export/` and needs the editor version's .NET export templates on the machine (ADR 0011); it isn't part of `just ci`. CI and the pre-commit hook call the tools directly, so they don't need `just`.

---

## 13. Developer wiki

- The wiki is **generated from the code** by `tools/__GAME__.Wiki` and built with DocFX (ADR 0008). Every reference page derives from a registry in code, so it can't disagree with the game: content kinds and definitions, `…Settings` tuning records, Core enums, the input map, dev flags, and localization status; plus the Core API reference and `docs/` read in place.
- **Never edit a generated page; edit its source** (content, a doc comment, a PO string, a settings record, the input map, a doc). Generated output is gitignored.
- **Strict mode** fails on a content error, a missing English string, a missing XML summary, or a registry mismatch; DocFX fails on broken links. CI runs both, so the wiki can't rot.
- A new registry (a console command table, an achievement list, a save schema) gets a page writer in the same change that adds it.

---

## 14. Git workflow

- Default branch `main`, protected; changes go through merge/pull requests with a green pipeline, squash-merged.
- Branches: `feat/…`, `fix/…`, `chore/…`, `docs/…`, `art/…`, `spike/…`.
- **Conventional Commits**, scoped by feature or Core area: `feat(combat): add parry window`.
- **Git LFS** for binary art and audio (`.gitattributes`). Run `git lfs install` once per machine.
- **Never commit** `.godot/`, `bin/`, `obj/`, `export/`, `TestResults/`, generated wiki output. **Do commit** `*.import` and `*.uid`.
- Only commit when asked.

---

## 15. Definition of Done

A change is done when **all** of these hold:

- [ ] `dotnet csharpier check .` passes.
- [ ] `dotnet build __GAME__.sln` succeeds with **zero warnings**.
- [ ] All Core tests pass, and the Godot runtime tests pass where relevant.
- [ ] New or changed Core logic has unit tests; bug fixes have a regression test.
- [ ] Every new type and member has meaningful XML docs; every new shader has a header block.
- [ ] No hardcoded user-facing strings; new keys are in `en.po`, and in other locales as `#, fuzzy`.
- [ ] No new per-frame allocations in gameplay paths.
- [ ] Content validates; IDs are new or unchanged, never renamed.
- [ ] The wiki builds with `--strict`.
- [ ] Docs are updated if design, look, sound, or architecture changed (GDD, art direction, audio direction, standards, ADR), and the implementation plan checkboxes are ticked.
- [ ] Docs name implemented values by identifier instead of restating them, and references to a renamed or changed identifier are updated (§6.2).
