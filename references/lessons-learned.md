# Lessons learned

Each entry is a hard-to-catch gotcha that cost real time in the project this skill was distilled from (a Godot 4.8
.NET game built with Claude), with the rule that prevents it. The templates already apply every rule marked
**(templated)**, and the entry says why, so the guard isn't loosened later; the rest are for you to apply as the game
grows.

Add an entry only for a gotcha like that: easy to walk into, and costly unless planned for. When a change to the
templates or another reference solves a problem outright (a new doc, a scaffold file, a checklist item), make that
change and add no entry here. This file is read into context, so every entry has a cost.

## Contents

1. Project and build setup
2. Toolchain and CI
3. Testing
4. Godot C# runtime and performance
5. Architecture decisions that are expensive to change late
6. Content, localization, and docs
7. Working with Claude in these projects

---

## 1. Project and build setup

- **Godot.NET.Sdk compiles every `*.cs` under the project root.** Core, its tests, and tools got compiled into the
  game assembly (duplicate types, Godot leaking into Core). Rule: `<Compile Remove>` for every non-game folder in the
  game csproj. **(templated)**
- **The editor scans every folder and writes `.uid` files next to every `.cs` it sees,** including the Core test
  project, before `.gdignore` files existed. Rule: `.gdignore` in `src/<Core>`, `tests/<Core>.Tests`, `tools`, `docs`,
  `art-src` before the editor first opens the project; never on `tests/` itself, since `tests/godot` must be visible.
  **(templated)**
- **The default assembly name comes from the project name, spaces included.** Set `[dotnet] project/assembly_name`
  to the PascalCase identifier and write the csproj and sln by hand (or with the scaffold). **(templated)**
- **The target framework follows GodotSharp, not habit.** The design docs assumed `net8.0`; Godot 4.8's GodotSharp
  ships only `net10.0`. Check the package's `lib/` folders for the chosen Godot version before writing any project
  file.
- **Godot.NET.Sdk must equal the editor version exactly**, and dev builds change APIs between snapshots. Bump the
  editor, the Sdk, and CI's Godot download together, then run every gate. Prefer a stable Godot unless a needed
  feature is only in a dev build. **(templated: ADR 0002)**
- **A folder named after a Godot class shadows it.** `game/input/` makes `<Game>.Game.Input` a namespace, and every
  `Input.IsActionPressed` below it breaks. Name it `controls/`. Same for `os`, `time`, `engine`. **(templated)**
- **An empty `JsonSerializerContext` doesn't compile** (the source generator emits nothing for it). Create the
  content JSON context with the first content kind. **(templated: `ContentKinds` remarks)**
- **`using` order depends on the game's name** (`Godot` vs `<Game>` vs `System`), and StyleCop's SA1210 enforces it.
  Let CSharpier sort usings; the scaffold runs it last. **(templated)**
- **CSharpier also formats XML**, so a JUnit report written to the repo root (`godot-tests.xml`) fails
  `csharpier check`. Ignore test outputs in `.csharpierignore` and `.gitignore`. **(templated)**
- **XML comments can't contain `--`**, so a csproj comment quoting `dotnet test --project` breaks the project file
  (MSB4025). Keep command lines out of MSBuild comments.
- **Skill uploads drop dot-prefixed paths.** After one upload, every template file or folder starting with `.` was
  gone (the CI files, `.editorconfig`, the tool manifest, the hooks, every `.gdignore` and `.gitkeep`), along with
  folders that held only those, and the scaffold still reported success. Rule: store them as `_dot_<name>`, list every
  template file in `templates/MANIFEST.txt`, and let the scaffold fail when the template doesn't match it.
  **(templated)**

## 2. Toolchain and CI

- **A floating SDK image broke CI.** `dotnet/sdk:10.0` moved four feature bands ahead of the dev machine; the new
  analyzers (IDE0062) failed the warnings-as-errors build only in CI. Rule: exact SDK in `global.json`
  (`rollForward: latestPatch`) and the same exact image in CI; bump both in one commit. **(templated)**
- **Pin an SDK band every environment can install.** A Claude Code cloud container reached Ubuntu's archive
  (`apt-get install dotnet-sdk-10.0`, the 10.0.1xx band) but not Microsoft's download hosts, while `godot-builds`
  release downloads from GitHub worked. Pinning the 1xx band (`--dotnet-sdk 10.0.112`) let that container, CI
  (`setup-dotnet` with `global-json-file`) and the dev machine run identical gates; the dev machine installs that band
  side by side.
- **`! grep` can never fail a job under `set -e`.** The headless-boot error check needs an explicit
  `if grep …; then exit 1; fi`. **(templated)**
- **A YAML plain scalar containing `": "` parses as a mapping.** Use block scalars (`|`, `>`) for CI script lines
  with colons. **(templated)**
- **Godot scans an in-project NuGet cache** (CI points `NUGET_PACKAGES` inside the project so it can be cached).
  Touch `.nuget/.gdignore` before the headless import. **(templated)**
- **Godot on CI:** the official `godot-builds` release zip for Linux mono runs headless on stock .NET SDK images with
  `unzip wget libfontconfig1`. Fail on log lines starting `ERROR`, `SCRIPT ERROR`, `USER ERROR`. **(templated)**
- **`core.hooksPath` silently disables Git LFS hooks** installed in `.git/hooks`. Put LFS stand-in hooks in
  `.githooks/` next to the pre-commit hook. **(templated)**
- **BannedApiAnalyzers needs a documentation ID per overload** (`GD.Print(System.String)` and
  `GD.Print(System.Object[])` separately). Banning `System.Console` as a type fails the game build because Godot's
  generated code uses it; ban its methods instead. Prove each new entry by making the build fail once. **(templated)**
- **StyleCop vs CSharpier:** about twenty StyleCop layout rules contradict CSharpier's output; turn them off (list in
  ADR 0004), and also IDE0055. Some constructs CSharpier wraps in ways StyleCop rejects (SA1003 on a cast that ends a
  line); write them differently (a helper, a typed field) rather than suppressing. **(templated)**
- **Run the editor's console build** for CLI work on Windows (`…_console.exe`): it prints to the terminal and returns
  exit codes. Claude's shell doesn't inherit `GODOT_BIN`; record the path in CLAUDE.md or memory.

## 3. Testing

- **Third-party Godot test frameworks lagged** the Godot and .NET versions in use. An in-repo runner of about 400
  lines (discovery by attribute, a container node per test, frame waits, JUnit output) was enough and follows dev
  builds. **(templated: ADR 0006)**
- **Engine errors that don't throw** (a missing resource, a shader compile error, a script error in a callback) pass
  silently unless captured. Register a `Logger` with `OS.AddLogger` (Godot 4.5+) and fail the test on any error logged
  during it or in the frame after. **(templated)**
- **The runner itself must not allocate while a test measures allocations.** `await` allocates; race the test against
  a single timer instead of polling per frame. Make it a real-time one (`Task.Delay`): the tests run at `--fixed-fps`,
  where a `SceneTreeTimer` counts game time and a 10 s timeout fires within milliseconds. **(templated)**
- **The 0 B/frame rule needs a test, not an overlay.** The debug overlay sampled at 4 Hz and missed a string formatted
  on every kill; the first runtime test caught it. Fix: pre-build counter texts at load. **(templated: an allocation
  test per screen)**
- **Allocation tests were flaky under coverage instrumentation** (a one-time allocation on a first-time path). Take
  the lowest of up to three runs; a real per-call allocation is never zero. **(templated)**
- **A runtime test failed at random with `Handle is not initialized`** (from `SwapGCHandleForType` or
  `SetGodotObjectPtr`), and passed alone and on reruns. It's a race in Godot's C# glue (godot#112067, open as of
  4.8-dev7): a resource held only by a dropped C# wrapper is freed by the wrapper's finalizer some time after the GC
  collects the wrapper, and a load that takes it from the resource cache in between breaks its binding. Tests that
  loaded scenes with `ResourceLoader` and dropped them fed it, through the materials and sub-scenes their views hold.
  Hold loaded scenes for the run, finalize between tests, and check with `--gc-stress`; game code holds what it loads
  by path. **(templated: ADR 0006, Standards §5.8)**
- **Check project files as data, in Core tests:** the input map (every action has keyboard/mouse and controller
  bindings, and matches the constants), scene text keys, PO catalogs, registered catalogs, generated schemas. They
  run in milliseconds anywhere and catch most "forgot to update X" mistakes. **(templated)**
- **xUnit v3 on .NET 10 needs the Microsoft.Testing.Platform opt-in** in `global.json`, and runs with
  `dotnet test --project <path>`. **(templated)**
- **Golden tests for RNG and generators** need values from an independent implementation (a small Python port),
  not from the code under test.

## 4. Godot C# runtime and performance

- **`_Input` / `_UnhandledInput` overrides allocate a managed wrapper per event.** Poll `Input` and `DisplayServer`
  for continuous input (mouse motion) in gameplay. Overrides are fine on menus.
- **Implicit `string` → `StringName` allocates on every call.** Cache every action, animation, shader parameter,
  and group name as `static readonly StringName`. **(templated: `InputActions`)**
- **Godot's C# physics queries return `Godot.Collections` dictionaries** (allocating), and one `Area3D` per
  projectile didn't scale. A Core collision world (spheres, boxes, a grid broadphase, swept tests) measured 0.13 ms
  per tick and 0 B for 1,500 projectiles. Decide with a spike.
- **A C# `CompositorEffect` allocated 517 B per frame** even with an empty callback (argument marshalling). One canvas
  uber shader below the HUD did the whole post stack for +0.03 ms.
- **Physics interpolation and teleports:** after a teleport, `ResetPhysicsInterpolation()`. An origin rebase is not a
  teleport: resetting there dropped one interpolated step and showed as a hitch at high refresh rates. A camera that
  takes over in a rebase tick must shadow the current camera first.
- **Views must set local transforms** (`Position`, `Transform`), never `GlobalTransform`, or moving a stage root
  (rebasing, streaming) breaks them. Retrofitting this is a sweep through every view; start with it.
- **Rebase by whole grid cells and translation only**, or world-anchored patterns jump and world-space shading changes.
- **Streaming:** building a large level on the main thread took 318 ms. Preparing data on a worker (83 ms, off-thread)
  and uploading engine objects in steps of about 0.2 ms within a 1.5 ms per-frame budget kept every frame under
  16.7 ms. Sessions should bind to prepared data rather than rebuild it.
- **Shared resources:** mutating a loaded material changes every user. Use `instance uniform`s; duplicate once at
  load only when unavoidable.
- **The movie writer** is the way to see the game without a person: `--write-movie <dir>/f.png --fixed-fps 60
  --quit-after N -- --autopilot`. It opens a window, runs slower than real time, writes `f.wav` too, and its dummy
  audio driver logs harmless leaked `AudioStreamPlayback` errors at exit; mute audio in headless runs so CI stays
  clean.
- **Main-thread only:** Godot objects can't be touched from workers; marshal results with `CallDeferred` or a queue.

## 5. Architecture decisions that are expensive to change late

- **Transitions are an architecture decision.** The project built scene swaps with fades, then the first end-to-end
  playtest asked for one continuous world with no loading screens. Retrofitting it took a spike and about ten ADRs
  (streaming, origin rebase, in-world menus, camera hand-overs). Decide in planning whether the game is scene-based or
  continuous, and if continuous, build streaming, local transforms, and origin rebasing from Phase 1.
- **Simulation order in one Core session**, not spread over node `_PhysicsProcess` methods: tests can pin it, and
  determinism holds. When a second stage kind appears, extract a shared session base before copying code (the
  extraction passed 1,070 unchanged tests).
- **The RNG algorithm, seed expansion, salts, and seed-code format are save and share contracts.** Choose once, pin
  with golden tests, never change without a migration. **(templated: ADR 0009)**
- **Content IDs and enum content names are permanent** once content or saves use them.
- **Renderer choice** needs measurements on the minimum-spec hardware. Until it's available, derive a dev-machine
  tripwire from the hardware ratio and re-run a benchmark flag at every phase exit.
- **Keep game rules out of node scripts from the first day.** Once rules live in nodes, they need the engine to test.

## 6. Content, localization, and docs

- **Unknown JSON properties must be errors.** A misspelled field otherwise silently takes its default.
  **(templated)**
- **Generate JSON Schemas from the content models** and fail a test when they're stale; never hand-edit them. Every
  content file declares its `$schema` for editor autocomplete. **(templated)**
- **Key-based PO from day one**, even with English only: retrofitting keys into hundreds of literals is miserable.
  Untranslated entries are fuzzy with an empty `msgstr`; never machine-translate silently. **(templated)**
- **A wiki written by hand, or built late on a side branch, drifts.** The project's wiki was built on its own branch
  after three phases and was never merged into the active branch. Generate it from the code, build it in CI with
  `--strict`, and land it in Phase 0 so it grows with the code. **(templated: ADR 0008)**
- **Docs updated in the same change** as the design, look, or architecture change; implementation plan checkboxes
  ticked as work lands. The Definition of Done lists it so it isn't forgotten.
- **Keep CLAUDE.md's status short.** The project's status paragraph grew into one run-on sentence covering every
  landed feature. Keep three to five lines; history belongs in the plan and the ADRs.

## 7. Working with Claude in these projects

- **Hand-editing `.tscn`:** reference resources by `res://` path and never invent `uid://` values; Godot fills them on
  the next save. Exported node references use `node_paths=PackedStringArray("_field")` plus `_field = NodePath("…")`.
- **Ask before adding packages, addons, autoloads, top-level folders, or projects**, and record each in an ADR.
- **Respect the user's git preference.** Some users do every git write themselves; give them the commands in
  separate code blocks instead of running them. Only commit when asked.
- **Prove gates by breaking them once** (a missing doc, a banned API, a misformatted file), then revert. A gate that was
  never seen failing might not be wired in.
- **Spikes end in numbers and an ADR.** "It seemed fast" isn't a decision record.
