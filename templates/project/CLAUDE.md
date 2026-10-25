# CLAUDE.md

**__TITLE__** is <!-- one sentence: genre, the player's role, the core verb --> built in Godot (.NET) and C#. <!-- Targets, minimum spec, input priority. -->

## Status

**Phase 0 (Foundation) is in progress.** Keep this section to three to five lines: the current phase, what just landed, and what's next. History belongs in [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md) and the ADRs, not here. Track progress and tick checkboxes in the plan. Decisions are recorded in [docs/adr/](docs/adr/README.md).

## Docs: read the relevant one before working in an area

| Doc | Read when |
|---|---|
| [docs/GDD.md](docs/GDD.md) | Gameplay, systems, content, and the **lexicon (§3), which is binding for all names** |
| [docs/ART_DIRECTION.md](docs/ART_DIRECTION.md) | Anything visual: shaders, materials, VFX, UI look, assets, performance budgets |
| [docs/AUDIO.md](docs/AUDIO.md) | Anything audible: music, sound effects, ambience, voice and its localization, UI sound, the mix and buses, the cue list |
| [docs/CODING_STANDARDS.md](docs/CODING_STANDARDS.md) | Full rules behind everything below |
| [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md) | What to build next; phase exit criteria; spikes |
| `docs/adr/` | Past architecture decisions. Add one for any new architectural choice. |

## Stack

- **Engine:** Godot __GODOT_RELEASE__ .NET, __RENDERER_FEATURE__ renderer, physics at 60 Hz with interpolation on.
- **Language:** C# (`LangVersion latest`, nullable, warnings as errors), target `__TFM__`, SDK `__DOTNET_SDK__` pinned exactly in `global.json` and CI. `__GAME__.csproj` pins `Godot.NET.Sdk/__GODOT_SDK__`; bump it together with the editor and CI's `GODOT_VERSION` ([ADR 0002](docs/adr/0002-sdk-pinning.md)).
- **Tests:** xUnit v3 on Microsoft.Testing.Platform for Core, coverlet.MTP for coverage. Godot-runtime tests use the in-repo headless runner in `tests/godot` ([ADR 0006](docs/adr/0006-godot-runtime-tests-and-ci.md)).
- **Quality tooling:** CSharpier (formatting); .NET analyzers, StyleCop, Roslynator, and BannedApiAnalyzers (linting).
- **Wiki:** generated from the code by `tools/__GAME__.Wiki`, built with DocFX ([ADR 0008](docs/adr/0008-developer-wiki.md)).
- **CI and VCS:** the CI pipeline in the repository, Git LFS for binary art and audio.

## Commands

Routine commands are [just](https://github.com/casey/just) recipes in the root `justfile` (Standards §12.7); `just` lists them. Copy `.env.example` to `.env` and set `GODOT_BIN` there (a Windows path goes in single quotes).

| Recipe | Does |
|---|---|
| `just restore` | Restore the local tools (CSharpier, DocFX) |
| `just build` / `just release` | Debug solution build / ExportRelease game build |
| `just test` / `just cover` | Core tests / with the 85% coverage gate |
| `just format` / `just format-check` | CSharpier format / check |
| `just godot-import` / `just boot` / `just godot-test` | Headless import / boot / runtime tests (`just godot-test --filter=Name`) |
| `just export` | Release builds into `export/` (`just export-windows`, `just export-linux`; needs the export templates) |
| `just wiki` / `just wiki-serve` | Strict wiki build / serve at `http://localhost:8080` |
| `just hooks` | Enable the git hooks once per clone |
| `just ci` | Every gate in CI order (run before declaring work done) |

The underlying commands, for reference or when `just` isn't installed:

```bash
dotnet tool restore
```
```bash
dotnet build __GAME__.sln
```
```bash
dotnet test --project tests/__GAME__.Core.Tests
```
```bash
dotnet csharpier check .
```
```bash
dotnet csharpier format .
```

Regenerate the content JSON Schemas after changing a content model with `UPDATE_CONTENT_SCHEMAS=1 dotnet test --project tests/__GAME__.Core.Tests` (a test fails while they're stale).

Build the developer wiki with `tools/wiki/build.ps1` (or `sh tools/wiki/build.sh`); add `-Serve` (`--serve`) to serve it at `http://localhost:8080` and `-Strict` (`--strict`) to fail on missing strings or docs, as CI does.

Enable the git hooks once per clone with `tools/setup-hooks.ps1` (or `sh tools/setup-hooks.sh`: a project scaffolded on Windows commits the scripts without the executable bit).

Godot commands use the **console** build of the editor (on Windows, the `_console.exe`; it prints to the terminal and returns exit codes). Set `GODOT_BIN` to it (in `.env` for `just`). <!-- On the main dev machine: <path> -->

```bash
"$GODOT_BIN" --headless --path . --import
```
```bash
"$GODOT_BIN" --headless --path . --quit-after 120
```
```bash
"$GODOT_BIN" --headless --path . res://tests/godot/test_runner.tscn
```

The first reimports assets (needed after adding files). The second boots the game headless; any `ERROR` line is a failure. The third runs the Godot-runtime tests (exit code 1 on failure; add `-- --filter=Name` or `-- --junit=report.xml`, or `-- --gc-stress` to hold C# wrappers unfinalized between tests, [ADR 0006](docs/adr/0006-godot-runtime-tests-and-ci.md)).

Exports use the committed `export_presets.cfg` (`Windows` and `Linux`, [ADR 0011](docs/adr/0011-export-presets.md)): `"$GODOT_BIN" --headless --path . --export-release Linux export/linux/__GAME__.x86_64` into an existing folder. They need the editor version's .NET export templates (Editor → Manage Export Templates), installed per machine and replaced with each Godot bump; a pack-only export (`--export-pack`) doesn't.

**Dev flags** (after `--`, listed in the wiki): `--autopilot` drives the game with scripted input; `--pseudoloc` turns on pseudolocalization.

**Visual check without a person:** render frames with the movie writer and look at the PNGs (it opens a window; the dummy audio driver's leaked `AudioStreamPlayback` errors at exit are harmless there):

```bash
"$GODOT_BIN" --path . --resolution 1280x720 --write-movie "<scratch>/f.png" --fixed-fps 60 --quit-after 600 -- --autopilot
```

## Layout (abridged; full tree in [Standards §2](docs/CODING_STANDARDS.md#2-repository-layout))

- `src/__GAME__.Core/`: **pure C# game logic with no Godot references.**
- `tests/__GAME__.Core.Tests/`: xUnit tests mirroring Core, plus file-based checks of `project.godot`, scenes, PO files, and content (`TestSupport/`).
- `tests/godot/`: Godot-runtime tests and their runner.
- `game/<feature>/`: the Godot layer by feature (snake_case folders): scenes, node scripts, adapters.
- `content/`: JSON definitions loaded by Core, one folder per registered kind. `localization/`: PO files. `assets/`: imported models, audio, fonts. `shaders/`: gdshaders and materials.
- `dev/`: look-dev and sandbox scenes (not exported). `art-src/`: source art (LFS, `.gdignore`). `tools/`: the wiki generator and scripts.

## Architecture rules

1. **Dependencies run Game → Core only.** Core never references Godot and uses `System.Numerics`. The game layer converts types at the boundary with `ToGodot()` / `ToNumerics()`.
2. **Game rules live in Core and are unit-tested.** Godot nodes are thin views and controllers: they read input, call Core, and present the result. Any `if` about damage, cost, chance, or progression belongs in Core.
3. **No mutable static state.** The composition root is `game/app/Main`. Scenes get their dependencies through `Initialize(...)`, called **before** `AddChild`. At most one autoload, never a service locator.
4. **Call down, signal up.** Parents call methods on children; children raise C# events. Never `GetParent()` or absolute paths to reach other scenes.
5. **Determinism:** gameplay randomness comes only from Core `RngStream`s; visual-only randomness uses a cosmetic stream. Never `System.Random` or `GD.Rand*`. Time comes from `IClock`.
6. **Data-driven content:** JSON in `content/`, kinds registered in `ContentKinds.All`, IDs **permanent** (never rename or reuse). Tunable numbers go in content, not in `[Export]` fields.
7. **Views set local transforms** (`Transform`, `Position`), never `GlobalTransform`.
8. **Ask before** adding a NuGet package, a Godot addon, an autoload, a new top-level folder, or a new project. Record each in an ADR.

## Code rules (non-negotiable, enforced by the build)

- **Every type and member, including private methods, has XML docs** describing intent, units, and invariants. Overrides and Godot lifecycle methods use `<inheritdoc/>`. Private fields and test methods are exempt; test classes need a `<summary>`.
- **One type per file**, named after it; file-scoped namespaces; **sealed by default**; private fields `_camelCase`. Core namespaces follow folders; game namespaces follow the feature folder (`game/ui/title/` → `__GAME__.Game.Ui.Title`). Never name a game folder after a Godot class (`input`, `os`, `time`).
- **Nullable is on, and `!` is banned** except `[Export] private T _x = null!;` validated in `_Ready`.
- **Exceptions are for programmer errors only.** Expected failures use `TryX` or result types. No bare `#pragma warning disable`; use `[SuppressMessage(..., Justification = "...")]`.
- **Use the lexicon names** from GDD §3.
- **CSharpier owns layout.** Never hand-format against it.

## Godot C# rules

- Godot scripts are `partial`. Gameplay runs in `_PhysicsProcess`; `_Process` is for visuals. After teleporting a node, call `ResetPhysicsInterpolation()` (an origin rebase isn't a teleport).
- Prefer typed `[Export]` references over `GetNode("path")`; cache references in `_Ready`.
- **Store `StringName` and `NodePath` values in `static readonly` fields.** Input actions live in `game/controls/InputActions.cs`, with names in Core's `InputActionNames`.
- **Don't override `_Input` / `_UnhandledInput` for continuous input**: Godot allocates a wrapper per event delivered to C#. Poll `Input` instead.
- **C# events** by default; `[Signal]` only for editor wiring or `await ToSignal`. Unsubscribe in `_ExitTree`; subscribe with named methods, not lambdas.
- `QueueFree()`, never `Free()`. `IsInstanceValid` on references that may outlive their target. Pooled nodes are deactivated, never freed, during gameplay. Hold a resource loaded by path for as long as anything may load it again: one left to its C# wrapper's finalizer can break a later load (godot#112067, ADR 0006).
- Per-object material variation uses `instance uniform`. Never mutate a shared `Resource` at runtime.
- Pause uses `ProcessMode`. Godot objects are main-thread only; marshal worker results with `CallDeferred`.
- **Hand-editing `.tscn` / `.tres`:** reference resources by `res://` path and never invent `uid://` values. Commit `*.uid` and `*.import`; never commit `.godot/`.

## Performance: zero steady-state allocations per gameplay frame

None of these in per-frame paths: LINQ, capturing lambdas, string building or formatting, `new` on reference types, implicit `string` → `StringName`, `GetNode`, group queries, `Godot.Collections`, physics queries returning dictionaries, unguarded logging. Use pools and `MultiMeshInstance3D` for many instances. Budgets are in [Art Direction §14](docs/ART_DIRECTION.md#14-performance-budgets); runtime tests assert 0 B/frame.

## Testing

- New or changed Core logic gets xUnit tests; **Core line coverage stays ≥ 85%**; every bug fix gets a regression test.
- Name tests `Method_Scenario_Expected`, Arrange/Act/Assert. Deterministic: inject `IClock`, seed `RngStream`, never sleep.
- **Fakes, not mocks.** No FluentAssertions.
- Generators get invariant tests across many seeds; saves get round-trip and migration fixture tests; content is validated against the real `content/` folder; hot paths get allocation tests.
- **Godot runtime tests** (`[GodotTest]` in `tests/godot`) cover scenes loading, screens in the live tree (0 B/frame), and `res://` content. They load scenes through `context.Scene` or `context.Instantiate`, never `ResourceLoader`. A runtime test fails on any engine error it triggers.

## Localization (__LOCALES__)

- gettext PO files with **key msgids** (`ui.menu.start`); `localization/en.po` is the source of truth.
- **No user-facing string literals.** Code uses `L10nKeys` constants; scene labels contain the key with auto-translate on.
- **New keys** go into `en.po` and every other locale with an empty `msgstr` and `#, fuzzy`. Never machine-translate silently.
- Indexed placeholders (`{0}`) formatted with the locale's `CultureInfo`; plurals use `TrN`. The UI survives 35% text expansion and CJK line breaking.

## Visuals and shaders

- **Colors come from palette roles** (`pal_*` global shader uniforms), never raw colors. Readability beats style.
- **Every shader** starts with a header comment (purpose, uniforms with ranges and units); shared code goes in `.gdshaderinc`; it works on every supported renderer.
- Every new shader or material goes into the look-dev scene. Flash and shake effects scale with an intensity setting; nothing flashes full-screen faster than 3 Hz.

## Developer wiki

The wiki is generated from code: content kinds, `…Settings` records, Core enums, the input map, dev flags, and PO catalogs, plus `docs/` and the Core API. **Never edit generated pages; edit the source.** A new registry gets a page writer in the same change. CI builds it with `--strict`.

## Git

- Default branch `main`. Branches `feat/…`, `fix/…`, `chore/…`, `docs/…`, `art/…`, `spike/…`.
- **Conventional Commits**, scoped by feature or Core area: `feat(combat): add parry window`. Squash-merge once the pipeline is green.
- Only commit when asked.

## Definition of Done (check before declaring any task complete)

1. `dotnet csharpier check .` passes, and `dotnet build __GAME__.sln` has **zero warnings**.
2. All tests pass, and new Core logic is tested.
3. XML docs and shader headers are complete.
4. No hardcoded strings; new localization keys are in all PO files.
5. No per-frame allocations were introduced.
6. Content validates, and no IDs were renamed.
7. The wiki builds with `--strict`.
8. Docs are updated if the design, look, sound, or architecture changed (GDD, art direction, audio direction, standards, or an ADR), and the implementation plan checkboxes are ticked.
