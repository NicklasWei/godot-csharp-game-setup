# __TITLE__ — Implementation Plan

> **Status:** Living document. **Current phase: Phase 0 (Foundation).**
> Tick checkboxes as work lands (part of the [Definition of Done](CODING_STANDARDS.md#15-definition-of-done)).
> Related: [GDD](GDD.md) · [Art Direction](ART_DIRECTION.md) · [Coding Standards](CODING_STANDARDS.md)

---

## 1. Principles

1. **Feel first.** Phase 1 proves the core verb is fun before content scales. If it doesn't feel good, stop and iterate.
2. **Core logic is test-first.** Rules go into `__GAME__.Core` with tests from the start.
3. **Every phase ends playable,** with written exit criteria and a playtest where feel matters.
4. **Infrastructure early.** Localization keys, input parity, saves, the wiki, and performance budgets go in early, even while the content using them is placeholder.
5. **Decide the expensive things early.** Transitions (scene swaps or one continuous world), the renderer, collision, and the save model are planning decisions with spikes, not late surprises.
6. **Spikes answer questions with numbers.** Time-boxed; each ends in an ADR with the measurements.
7. **An MVP line.** Everything past it goes to the backlog (§5) until the MVP plays end to end.

---

## 2. Milestone overview

<!-- Rename and reshape the phases after Phase 1 to fit the game; keep each one playable with exit criteria. -->

| Phase | Goal | Exit criteria (summary) |
|---|---|---|
| **0 · Foundation** | Tooling, structure, CI, docs | All gates enforced in CI; the main scene runs; the wiki publishes |
| **1 · Feel slice** | The core verb feels great | A short playable slice at the frame budget, 0 B/frame, on the autopilot in CI; a feel playtest passes |
| **2 · Core rules** | Systems in Core, content kinds, RNG streams | The slice runs on Core systems and content; Core coverage ≥ 85% |
| **3 · Structure** | The session loop end to end | One full session plays start to finish, seed-reproducible |
| **4 · Meta and front end** ⟵ **MVP** | Menus, settings, saves, progression | Complete loop: play → finish → progress → play again; saves round-trip |
| **5 · Art and tech-art** | Full visual style | Look-dev QA passes; budgets hold on minimum-spec hardware |
| **6 · Content** | Content to release targets | GDD §16 targets met |
| **7 · Audio** | Music and SFX | Every event has sound; mix passes |
| **8 · Localization and accessibility** | All locales, accessibility settings | All locales complete; accessibility checklist passes |
| **9 · Polish and ship** | Performance, stability, exports | Release candidates for every platform |

---

## 3. Spikes

Each spike is time-boxed and ends in an ADR.

| ID | Question | Phase | Time box | Decision recorded |
|---|---|---|---|---|
| **S1** ✅ | Which Godot-runtime test approach works with this Godot and .NET version? | 0 | — | ADR 0006: the in-repo headless runner (from the template) |
| **S2** | Can CI run headless Godot .NET (download, import, boot, runtime tests) on the chosen host? | 0 | 1 day | ADR 0006 once the pipeline is green |
| **S3** | Does the chosen renderer fit the minimum-spec budget with the planned look? | 1 | 1 day | |
| <!-- S4 --> | <!-- e.g. collision approach at the expected entity count; streaming if the world is continuous; save model --> | | | |

---

## 4. Phase details

### Phase 0 · Foundation

**Goal:** a repository where the standards are enforced by tools before any game code exists.

**Scaffolded (verify each once):**
- [ ] `dotnet build __GAME__.sln` has zero warnings; a missing XML doc, `System.Random` in Core, and a misformatted file each fail the gate (then revert them).
- [ ] `dotnet test --project tests/__GAME__.Core.Tests` passes, with Core coverage ≥ 85%.
- [ ] The Godot editor opens the project and runs `game/app/main.tscn`; `--headless --import`, the headless boot, and the runtime tests pass with no `ERROR` lines.
- [ ] The wiki builds with `--strict` and DocFX `--warningsAsErrors`.

**Repository:**
- [ ] `git init`, branch `main`, `git lfs install`, `tools/setup-hooks` run once per clone.
- [ ] Remote created; `main` protected; merges require a green pipeline.
- [ ] CI pipeline green on the host, including `godot-smoke` (spike S2) and the wiki publish (Pages limited to project members if private).
- [ ] The first export includes Core and `content/*` (with the first export presets).

**Design docs:**
- [ ] GDD: overview, pillars, lexicon, loops, controls, and open questions filled in.
- [ ] Art Direction: keywords, readability rules, palette roles, performance budgets.
- [ ] This plan: phases after Phase 1 reshaped for the game; spikes listed.
- [ ] ADRs for any decision already made beyond the template's (renderer, 2D/3D, transitions, platforms).
- [ ] `CLAUDE.md` status updated to Phase 1.

**Exit criteria:** CI is green; the gates demonstrably fail on violations; the editor runs the main scene; the docs above exist.

---

### Phase 1 · Feel slice

**Goal:** one small, hand-built slice that proves the core verb.

- [ ] The core verb's simulation in Core (a session stepping every system in a fixed order), with tests.
- [ ] One gameplay node that reads input, steps the session, and feeds views.
- [ ] Both input devices working; prompt glyphs follow the last-used device.
- [ ] `--autopilot` drives the slice; CI runs it headless; the movie writer captures it.
- [ ] Debug overlay: FPS, frame times, allocated bytes per frame.
- [ ] A runtime test runs the slice at 0 B/frame.
- [ ] Benchmark flag measuring against the §14 budgets (spike S3).
- [ ] Placeholder audio and the post stack, if they matter to feel.

**Exit criteria:** the slice runs at the frame budget with 0 B/frame, and a feel playtest passes.

<!-- Later phases: list their tasks and exit criteria here as they come up. -->

---

## 5. Backlog (post-MVP)

<!-- Ideas that are explicitly out of the MVP. -->
