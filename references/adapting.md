# Adapting the template to the game

The template is genre-neutral: it decides tooling, structure, and quality gates, and leaves gameplay architecture to
the game. This file covers the decisions that depend on the game, what the template assumes for each, and what to change.

## Contents

1. Defaults that hold for every game
2. Decisions that depend on the game
3. What to remove or relax, and how
4. Retrofitting an existing Godot C# project

---

## 1. Defaults that hold for every game

Keep these unless an ADR says otherwise; they cost little and pay off in every genre:

- The engine-free Core and a thin Godot layer, with Core at ≥ 85% line coverage.
- Warnings as errors, full XML docs, banned APIs, CSharpier, exact SDK pinning.
- Key-based PO localization, even for an English-only game.
- Input parity tests (or, for a touch- or mouse-only game, a narrower rule recorded in an ADR).
- Content as JSON in registered kinds with permanent IDs and generated schemas.
- Seeded `RngStream`s for gameplay randomness.
- The in-repo runtime test runner, headless Godot in CI, and the generated wiki.

## 2. Decisions that depend on the game

| Decision | Template default | When to change, and what to do |
| --- | --- | --- |
| **2D or 3D** | 3D-leaning settings (Jolt for 3D physics, `Vector3` interop) | For 2D, set the stretch mode and texture filtering in `project.godot` (pixel art: `canvas_items` or `viewport` stretch, nearest filtering), keep Godot 2D physics, and drop unused interop. |
| **Renderer** | `--renderer forward_plus` | `mobile` for mid-range or handheld targets; `gl_compatibility` for low-end, web, or many 2D games. Measure on the minimum spec (spike), record an ADR, and keep shaders working on the fallback. |
| **Frame-rate and allocation budget** | 60 fps, 0 B per gameplay frame | A turn-based or menu-heavy game can relax 0 B/frame for non-gameplay screens; keep it for any real-time loop. Write the relaxation into Coding Standards §8 and an ADR. |
| **Determinism** | Seeded streams, banned `System.Random` | Always keep the streams. If players share seeds or replays, add a seed-code format and golden tests, and treat the generator as a save contract (ADR 0009). |
| **Gameplay collision** | Not decided | Few bodies: Godot physics behind the stage controller. Hundreds of projectiles or strict determinism: a Core collision world. Decide with a spike (Coding Standards §3.10). |
| **Transitions** | A title screen in the main scene; scene swaps later | A continuous world needs streaming (worker-prepared data, budgeted uploads), local transforms everywhere, and an origin rebase strategy from Phase 1. Decide in planning. |
| **World size** | Not decided | Large worlds need origin rebasing (whole grid cells, translation only, no interpolation reset) or double precision builds; decide early. |
| **Content kinds** | None registered | Add kinds as the GDD's §10 names them (`ContentKinds` remarks list the steps). |
| **Locales** | `--locales` (English first) | Adding a locale later is a PO file plus registration; the tests find it. CJK needs fonts with coverage and the ICU data (already on). |
| **Input** | Controller and keyboard/mouse parity | Touch or mouse-only games: replace the parity tests with the rule that fits, in an ADR. Keep all action names in `InputActionNames`. |
| **Code host and CI** | `--ci gitlab` or `--ci github` | Other hosts: port the jobs one-to-one (format, build Debug and ExportRelease, Core tests with coverage, wiki strict, godot-smoke, pages). |
| **Platforms and exports** | Windows and Linux x86_64 presets in `export_presets.cfg`, `just export` (ADR 0011) | Another platform gets a preset with the same filters, in an ADR (macOS needs a bundle identifier and signing; consoles, their own SDKs). Drop a preset the game won't ship, with its recipe. Add export jobs on tags with the export templates cached. A new root JSON file or a Godot-visible folder that shouldn't ship goes into every preset's exclude filter. |
| **Saves** | Rules in the standards, no code yet | Implement `IFileStore`, the versioned envelope, atomic writes, and migration fixtures with the first persisted data. |
| **Multiplayer** | Out of scope | Needs its own ADRs before Phase 1 (authority model, determinism, rollback); the Core/Game split helps either way. |
| **Art import pipeline** | None | If the style needs mesh processing or strict naming, write an `EditorScenePostImport` (under `#if TOOLS`) whose logic lives in Core, with a Blender-scripted fixture and a runtime test; reject bad assets loudly. |

## 3. What to remove or relax, and how

- **Never silently.** Every relaxation is a change to `docs/CODING_STANDARDS.md` plus an ADR, in the same change.
- **Removing a locale:** delete its PO file and its `project.godot` registration together.
- **Dropping the GitHub or GitLab files:** the scaffold already keeps only the chosen host.
- **Removing the wiki** is possible but loses the parity guarantee; prefer keeping it, since it costs one CI job.
- **Coverage threshold:** keep 85% on Core. Generated or trivially declarative code can be excluded with
  `[ExcludeFromCodeCoverage]` plus a justification, never by lowering the threshold.
- **The sample title screen** is a placeholder: replace it with the real front end, keeping the patterns (typed
  exports, `Initialize` before `AddChild`, keys instead of text, an allocation test).

## 4. Retrofitting an existing Godot C# project

When the user wants an existing project brought up to this standard, don't scaffold over it (`--overwrite` is for a
fresh project the editor just created, not for a codebase with its own C#, csproj and settings). Instead, in this order,
one reviewable change at a time:

1. **Pin the toolchain:** `global.json`, `Directory.Build.props` (start with `TreatWarningsAsErrors` off, then fix
   and turn it on), `Directory.Packages.props`, `.editorconfig`, `stylecop.json`, CSharpier (one formatting-only
   commit).
2. **Split Core out:** a `src/<Game>.Core` project with `.gdignore` and `<Compile Remove>`, then move pure logic into
   it piece by piece, each with tests. Add `CoreArchitectureTests`.
3. **Tests:** the Core test project, then the file-based project tests (input map, localization), then the runtime
   runner and a scene smoke test.
4. **Banned APIs**, one list at a time, fixing the findings.
5. **Localization keys:** move literals to keys area by area; the orphan and coverage tests keep it honest.
6. **CI**, then the wiki, then the docs and ADRs for what was decided along the way.

Copy individual files from `templates/project/`, replacing `__GAME__` and the other tokens by hand, and renaming each
`_dot_` prefix back to `.` (`_dot_editorconfig` is `.editorconfig`; the template can't store dotfiles, see SKILL.md).
