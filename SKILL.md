---
name: godot-csharp-game-setup
description: >-
  Startup guide and scaffold for new Godot 4 games in C# (.NET), any genre. Covers the interview, design docs (GDD
  with a binding lexicon, art direction with budgets, phased plan, ADRs) and a verified repo scaffold: engine-free
  Core library with a thin Godot layer, warnings-as-errors analyzers, CSharpier, banned APIs, xUnit v3 with a coverage
  gate, file-based tests of project.godot, scenes and PO files, an in-repo Godot runtime test runner with a 0 B/frame
  check, headless Godot in GitLab or GitHub CI, key-based PO localization, JSON content kinds with generated schemas,
  seeded RNG, and a developer wiki generated from the code (DocFX). Use it whenever the user wants to start, plan,
  prototype, or scaffold a new game in Godot with C#, asks how a Godot .NET repo should be structured or managed,
  wants a "proper" or "strict" game project, or wants an existing Godot C# project brought up to that standard, even
  if they don't name these tools.
---

# Godot C# game setup

This skill turns "let's make a game" into a strictly managed Godot 4 (.NET) repository: design docs, a phased plan,
recorded decisions, and a scaffold where formatting, docs, tests, localization, content validation, allocation budgets,
and a generated wiki are all enforced by tools from the first commit.

Everything here was distilled from a real Godot 4.8 .NET game built with Claude over several phases (DATA DRIFT), and
the scaffold is verified end to end (build, tests, coverage, headless Godot, wiki). It makes no assumption about genre:
it decides tooling, structure, and quality gates, and leaves gameplay architecture to the game's own ADRs.

## Workflow

1. **Interview** the user: only what can't be defaulted. The questions and defaults are in
   [references/planning.md](references/planning.md) §1. At minimum before scaffolding: title and PascalCase
   identifier, Godot version, code host (GitLab or GitHub), languages, 2D or 3D and the renderer.
2. **Check the toolchain** on this machine:
   - `dotnet --list-sdks`: a .NET SDK whose major version matches the target framework.
   - The target framework must be at least what that Godot version's `GodotSharp` package targets (Godot 4.8 ships only
     `net10.0`). The default `net10.0` was verified with Godot 4.5.1, 4.6.1, and 4.8-dev7.
   - The Godot .NET editor of the chosen version, and its **console** build path for `GODOT_BIN`.
   - Godot must be 4.5 or later (the runtime test runner uses `OS.AddLogger`).
   - `just --version`: the command runner behind the project's short commands (`winget install Casey.Just`,
     `brew install just`, `apt install just`). Optional for the scaffold itself; every recipe has a raw command too.
3. **Scaffold** into an empty folder (an initialized git repo is fine):

   ```bash
   python <skill-dir>/scripts/scaffold.py --name StarForge --title "Star Forge" --dest <path> --godot 4.5.1-stable --locales en,es,de --ci gitlab --renderer forward_plus
   ```

   `--godot` takes a `godotengine/godot-builds` release tag (`4.5.1-stable`, `4.8-dev7`); the script derives the
   Godot.NET.Sdk version from it. `--dotnet-sdk` defaults to the newest installed SDK matching `--tfm` (default
   `net10.0`). `--ci` is `gitlab`, `github`, or `none`; `--renderer` is `forward_plus`, `mobile`, or
   `gl_compatibility`. Prefer an empty folder; it refuses a non-empty one unless you pass `--overwrite`, which
   replaces same-named files, keeps the rest, and lists what it replaced. If the user already created a project in the
   Godot editor, commit it first, scaffold with `--overwrite`, then review `git diff` and merge their settings back
   into `project.godot` (icon, rendering driver, physics engine, and similar). Standard library Python only. It creates the solution,
   restores the local tools, and runs CSharpier (which also sorts `using`s for the game's name).
4. **Verify** with [references/verification.md](references/verification.md): every gate green, then each gate broken
   once on purpose and reverted. Report results faithfully, including anything you couldn't run.
5. **Fill the docs** (GDD, Art Direction, Implementation Plan) and write ADRs for decisions already made, following
   [references/planning.md](references/planning.md). Update the project's `CLAUDE.md` (one-line description, status,
   `GODOT_BIN` path).
6. **Hand off what needs the user's accounts**: creating the remote, protecting `main`, enabling Pages. If the user
   does git writes by hand (check memory or ask), give the git commands as separate code blocks instead of running them.

After that, development follows the project's own `CLAUDE.md` and `docs/CODING_STANDARDS.md`, which the scaffold wrote.

## What the scaffold creates

```
CLAUDE.md                     The project's rules and commands for every future session (keep its status short)
global.json                   Exact .NET SDK, latestPatch; Microsoft.Testing.Platform opt-in
Directory.Build.props         latest C#, nullable, warnings as errors, analyzers, docs required, strict
Directory.Packages.props      Central package versions
.editorconfig, stylecop.json  Analyzer severities; StyleCop rules that fight CSharpier turned off
BannedSymbols.*.txt           No System.Random, wall clock, Console, GD.Print, GD.Rand*, Thread.Sleep
.config/dotnet-tools.json     CSharpier and DocFX, pinned
.githooks/, tools/setup-hooks pre-commit (format, build, Core tests) plus Git LFS stand-in hooks
justfile, .env.example        Short commands (just build, just test, just ci, ...); GODOT_BIN from a git-ignored .env
.gitlab-ci.yml | .github/     format, build (Debug + ExportRelease), Core tests + coverage, wiki, godot-smoke, pages
project.godot                 Assembly name, physics interpolation, input map (pause), translations, renderer
<Game>.csproj                 Godot.NET.Sdk pinned; excludes Core, tests, tools; runtime tests not in ExportRelease
src/<Game>.Core/              Diagnostics (ILog, IClock), Randomness (RngStream), Content (kind registry, loader,
                              database), Localization keys, InputActionNames, DevFlags
tests/<Game>.Core.Tests/      90 tests: content pipeline, schemas, localization catalogs, input map, RNG reference
                              vectors, architecture guards (no Godot in Core, no mutable statics)
tests/godot/                  In-repo runtime test runner + scene smoke, main boot, title 0 B/frame, res:// content
game/                         Main (composition root), AppServices, GodotLog, GodotClock, LocaleService,
                              ContentFiles, NodeGuard, NumericsInterop, InputActions, a placeholder title screen
localization/                 en.po plus one fuzzy PO per locale
content/schemas/              Generated JSON Schemas (none until the first content kind)
tools/<Game>.Wiki, tools/wiki Wiki generator and DocFX site; .claude/launch.json previews it
docs/                         GDD, ART_DIRECTION, CODING_STANDARDS (the binding rules), IMPLEMENTATION_PLAN,
                              adr/ 0001–0010 recording the template's own decisions
```

## The rules that matter most

The full text is the scaffolded `docs/CODING_STANDARDS.md`; these are the ones to keep in mind when planning:

- **Game rules live in Core, unit-tested; Godot nodes are views.** Core never references Godot. A stage's simulation
  is one Core session stepped in a fixed order; nodes read input, call it, and present it.
- **Dependencies flow down:** `Main` builds services and passes them through `Initialize(...)` before `AddChild`. No
  mutable statics, at most one autoload, no service locator. Call down, signal up with C# events.
- **Tools enforce everything they can:** warnings as errors, XML docs on every member (they become the wiki), banned
  APIs, CSharpier, exact SDK pins in global.json and CI.
- **Tests cover the project files too:** the input map's parity, scene text keys, PO catalogs, content, and schemas
  are checked as data in milliseconds; the runtime runner fails on any engine error and checks 0 B/frame.
- **Deterministic by default:** seeded `RngStream`s per consumer; time through `IClock`.
- **Data-driven:** content kinds registered once in `ContentKinds.All`; IDs permanent; unknown JSON properties are
  errors; schemas generated; tunables in content, not in `[Export]`s.
- **Localized from day one:** stable PO keys, `en.po` as the source of truth, fuzzy until translated.
- **Zero managed allocations per gameplay frame**, enforced by runtime tests.
- **The docs can't drift:** the wiki is generated from code and built with `--strict` in CI; design docs and ADRs are
  updated in the same change; spikes end in ADRs with numbers.

## Lessons that cost the most

All of them, with the stories and fixes, are in [references/lessons-learned.md](references/lessons-learned.md). The ones
to raise during planning, because they're expensive to change later:

- **Decide transitions up front**: scene swaps vs one continuous world. Retrofitting continuity took a spike and about
  ten ADRs. If continuous: streaming, local transforms, and origin rebasing from Phase 1.
- **Views set local transforms, never `GlobalTransform`**, and physics interpolation is on from the start.
- **The minimum-spec hardware sets every budget**: measure the renderer there (or derive a tripwire) early.
- **Collision at scale**: Godot's C# physics queries allocate; hundreds of projectiles want a Core collision world.
- **The RNG and content IDs are save contracts**: choose once, pin with golden tests, never rename.
- **Land the wiki in Phase 0.** The source project built its wiki late on a side branch, and it was never merged.
- **Pin the SDK exactly**: a floating CI image brought new analyzers that failed only in CI.
- **Measure allocations with tests**, not the overlay: a per-kill string format slipped past 4 Hz sampling.

## Recipes for a scaffolded project

- **A content kind:** follow the steps in `ContentKinds`' remarks (document, `ContentJsonContext`, definition record,
  `ContentKind` with validator and keys, registration), then
  `UPDATE_CONTENT_SCHEMAS=1 dotnet test --project tests/<Game>.Core.Tests`, add content files with `$schema`, and add
  the keys to every PO file (fuzzy outside English). The tests name anything missing; the wiki page appears by itself.
  The very first kind also creates `ContentJsonContext` (an empty one doesn't compile).
- **A tuning record still in code:** a public `…Settings` record with a static `Default`; the wiki's tuning page lists
  it. Move it into content when it stabilizes.
- **A screen or stage:** a scene with a root script, typed `[Export]`s validated with `NodeGuard`, `Initialize(...)`
  for dependencies, keys for all text, a runtime test that it shows, and an `AllocationProbe` test that it runs at
  0 B/frame.
- **An input action:** add it to `project.godot` with keyboard/mouse **and** controller bindings, a constant in
  `InputActionNames` (and `All`), a `StringName` in `InputActions`, and a row in GDD §6.
- **A dev flag:** a property on `DevFlags` plus its `…Argument` constant and a `Parse` case, a test, and its use in the
  game; the wiki pairs them.
- **A locale:** a PO file with every key (fuzzy, empty `msgstr`, correct `Plural-Forms`) and its registration in
  `project.godot`; the tests find it.
- **A dependency, autoload, top-level folder, or project:** ask the user first, then record an ADR.
- **A gameplay feature, end to end:** GDD numbers → Core types and tests → content → view nodes → runtime test → docs
  and plan checkboxes → Definition of Done.
- **Seeing the game without a person:** the movie writer with `--autopilot` (Coding Standards §9), then read the PNGs.

## Reference map

| File | Read when |
|---|---|
| [references/planning.md](references/planning.md) | Before scaffolding (the interview) and when writing the GDD, art direction, plan, ADRs, CLAUDE.md |
| [references/adapting.md](references/adapting.md) | The game differs from the defaults (2D, renderer, collision, continuous world, multiplayer, input, exports), or retrofitting an existing project |
| [references/verification.md](references/verification.md) | Right after scaffolding, and after upgrading Godot or the SDK |
| [references/wiki.md](references/wiki.md) | Working on the wiki: what each page comes from, strict checks, adding pages, DocFX problems |
| [references/lessons-learned.md](references/lessons-learned.md) | Planning, reviewing a design, or debugging something odd in Godot C#, CI, or tests |
| `templates/project/docs/CODING_STANDARDS.md` | The full rules the scaffolded project follows |
| `templates/project/` | Individual files to copy by hand (replace `__GAME__`, `__TITLE__`, and the other `__TOKENS__`, and rename `_dot_` to `.`) |

## Keeping this skill current

Pinned versions verified together (October 2026, on Windows): Godot 4.5.1-stable, 4.6.1-stable, and 4.8-dev7 with
.NET SDK 10.0.401 and `net10.0`; CSharpier 1.3.0, DocFX 2.81.0, StyleCop.Analyzers 1.2.0-beta.556,
Roslynator.Analyzers 5.0.1, BannedApiAnalyzers 5.6.0, xunit.v3 4.0.1, coverlet.MTP 10.1.0. Also verified on Linux in a
Claude Code cloud container (October 2026): Godot 4.8-beta1 with .NET SDK 10.0.112, GitHub variant. The CI templates are valid
YAML but weren't run on GitLab or GitHub from the template (the GitLab one mirrors a pipeline that runs green; the
GitHub one, with actions checkout/setup-dotnet v4, upload-pages-artifact v3, deploy-pages v4, is a port), so watch the
first pipeline. Newer versions are usually fine; after bumping any, scaffold a throwaway project and rerun
[references/verification.md](references/verification.md) before trusting it. When a scaffolded project learns
something the template should know, update the template and `references/lessons-learned.md` here too.

**Template files never start with a dot.** Skill uploads drop every path segment that starts with `.` (in one upload,
all 22 template dotfiles vanished and the scaffold still reported success). Store them as `_dot_<name>`, in folder
names too (`_dot_github/workflows/ci.yml`); `scaffold.py` restores the dot on copy. `templates/MANIFEST.txt` lists
every template file, and the scaffold refuses to run when one is missing or unlisted. After adding or removing a
template file, run `python scripts/scaffold.py --update-manifest`; before every upload, run
`python scripts/scaffold.py --check-template`.
