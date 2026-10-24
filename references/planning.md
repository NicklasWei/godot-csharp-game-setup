# Planning a new game project

How to go from "I want to make a game" to a repository with design docs, a phased plan, and recorded decisions. The
templates in `templates/project/docs/` are the skeletons; this file is how to fill them well.

## Contents

1. The interview
2. Order of work
3. Writing the GDD
4. Writing the Art Direction
5. Writing the Audio Direction
6. Phases and exit criteria
7. Spikes
8. ADR habits
9. CLAUDE.md

---

## 1. The interview

Ask only what can't be defaulted, in one round if possible (use the question tool with options where the choices are
known). Group the questions; don't interrogate. Record every answer in the GDD or an ADR, and every unanswered one in
GDD §17 (open questions).

**Needed before scaffolding** (they become scaffold arguments or files):

| Question | Why it matters | Default if the user has no preference |
| --- | --- | --- |
| Working title, and a PascalCase identifier | Assembly, namespaces, solution name | The title without spaces |
| Godot version | Godot.NET.Sdk version, target framework, CI download | The latest **stable** 4.x (4.5 or later); a dev build only for a needed feature |
| Code host | CI template (GitLab or GitHub) and Pages | Ask; there's no safe default |
| Languages | PO catalogs, plural forms, font coverage | English only, with keys from day one |
| 2D or 3D, and the renderer | `project.godot`, budgets, shader rules | 3D: Forward+ (Mobile as fallback); 2D or low-end targets: Compatibility or Mobile |

**Needed before Phase 1** (they go into the GDD, the Art Direction, and the Audio Direction):

- Genre, pitch, 3–5 design pillars, and non-goals.
- Platforms and the **minimum-spec hardware** (it sets every performance budget).
- Input: controller-first, mouse-first, or touch; is full parity required?
- Session shape: how long a session is, how it starts and ends, what persists between sessions.
- **Transitions:** scene swaps with loading, or one continuous world? (Expensive to change later; see lessons §5.)
- Determinism needs: seeds shared by players? replays? procedural generation? (The RNG streams are in either way.)
- Expected scale: entity and projectile counts, world size (collision approach, floating-point range, streaming).
- Online features or multiplayer (out of this template's scope; needs its own ADRs early).
- Art pipeline: hand-made models or sprites, procedural, source tools, licensing.
- **Sound** (the Audio Direction, §5 below). A setup isn't complete without it, and "none" is an answer while a blank
  is not:
  - Music: genre and style, mood, and 2–4 references (artists, albums, tracks, game or film scores), each with what to
    take from it; original, licensed, library, or procedural; linear or adaptive; or no music at all.
  - Sound effects: the style (realistic, stylized, synthesized, retro or chiptune) and any signature sounds.
  - Ambience: what the world sounds like between events.
  - **Voice:** the voice model (full voice acting, partial, barks only, efforts only, gibberish, sound-effect speech
    such as text blips, synthesized, or none), and if anything is spoken, which languages are recorded (one language
    subtitled everywhere, or dubbed per locale). Ask it next to the languages question; the answers depend on each
    other.
- Accessibility goals, including for sound (subtitles, captions, visual cues for important sounds).

Don't ask about things the standards already decide (testing stack, analyzers, formatting, folder layout). Mention
them as defaults, and change one only with an ADR.

## 2. Order of work

1. **Interview** (above).
2. **Scaffold** (`scripts/scaffold.py`) into an empty folder or a fresh repository.
3. **Phase 0 verification** (`verification.md`): every gate green, and each gate proven to fail once.
4. **Fill the docs** in this order: GDD §1–§4 and the lexicon → Art Direction §1–§4 and §14 budgets → Audio
   Direction §1–§8 and §10.1 buses → the Implementation Plan's phases and spikes → ADRs for decisions already made
   (renderer, 2D/3D, transitions, scope).
5. **Update CLAUDE.md**: the one-line description, the status (Phase 1), `GODOT_BIN` on this machine.
6. **Set up the remote**: push, protect `main`, require a green pipeline, enable Pages (members only if private).
   These are the user's actions on their account; give them the steps.
7. Start Phase 1.

## 3. Writing the GDD

- Short declarative sentences. Every system gets numbers, marked *(tunable)*.
- **The lexicon (§3) comes first and is binding.** Every game concept gets one in-world term and one code identifier,
  and the code, keys, UI, and docs use them. Pick terms that read well as identifiers. Forbid the generic synonyms
  explicitly (for example, "Stamina, never Energy or Endurance").
- Controls (§6) mirror the input map: one row per action, both devices.
- Structure (§7) states the transition model.
- Content (§10) lists the content kinds with their ID prefixes; each becomes a registered kind in Core.
- Open questions (§17) are explicit, each with who decides and by when.

## 4. Writing the Art Direction

- Keywords and references first, then rules: visual hierarchy, **readability rules that beat style**, palette roles
  (global shader uniforms, never raw colors), shape language.
- **Performance budgets (§14)** for the minimum-spec target at its native resolution: frame rate, GPU and CPU frame
  times, draw calls, triangles, **0 B managed allocations per gameplay frame**, load times.
- A look-dev scene (`dev/lookdev/`) shows every material and shader; a file-based test fails when one is missing.
  Create it with the first shader.

## 5. Writing the Audio Direction

The Audio Direction (`docs/AUDIO.md`) is to sound what the Art Direction is to the look, and a setup isn't complete
without it. Many users aren't sound designers: explain the choices in plain words (the doc ends with a glossary) and
offer options rather than open questions.

- **Fill §1–§8 and the buses (§10.1) in Phase 0**, even when the answer is "no music" or "no voice acting". Mark the
  rest *(open)*, with the phase that decides it, and list it in §16.
- **References first:** real tracks, albums, or games, each with what to take from it (tempo, instrumentation, mood,
  a mix trait). They're for mood and principles, never for copying; temp tracks in prototypes never ship.
- **The voice model is a budget and localization decision**, not only a style one. Full voice acting brings a script,
  casting, sessions, subtitles, and per-language recording if dubbed; efforts, gibberish, or sound-effect speech need
  no translation. Record the choice and the voice languages in §7, and in an ADR if it shapes the pipeline (a line
  database, voice files keyed like PO keys).
- **Readability rules apply to sound too:** every gameplay-critical cue is both seen and heard, and the most repeated
  sounds stay soft and varied. Put them in §2, where the audio QA checks them.
- **Audio follows the same architecture:** sounds react to Core's per-tick events and never change Core state, variation
  uses the cosmetic random stream, and players with polyphony keep audio at 0 B/frame. Godot's own audio covers
  adaptive music (interactive, synchronized, and playlist streams); middleware such as FMOD or Wwise is an addon and
  an ADR.
- **The bus layout comes with the first sound** (Phase 1 placeholders): Master, Music, SFX, Ambience, UI, and Voice if
  anything is spoken, pinned by a file-based test.
- **Budgets and loudness** (§10.3, §14) are set for the minimum-spec device and measured in the audio phase.

## 6. Phases and exit criteria

The template's plan has ten generic phases; reshape everything after Phase 1 for the game, but keep the principles:

- **Phase 0 (Foundation):** tooling, CI, docs, wiki. Exit: every gate enforced in CI and proven to fail; the main scene
  runs; the docs exist.
- **Phase 1 (Feel slice):** the smallest playable slice of the core verb, hand-built, with placeholder art and audio.
  Simulation in Core, one gameplay node, both input devices, `--autopilot`, a benchmark flag, a 0 B/frame runtime
  test. Exit: a feel playtest passes at the frame budget. If it doesn't feel good, iterate here; don't scale content.
- **Then:** rules engine and content kinds → the full session loop → meta, menus, settings, and saves (the **MVP
  line**) → art pass → content → audio (final music, effects, ambience, voice, and the mix, to the Audio
  Direction) → localization and accessibility → polish and ship.
- Every phase: a goal sentence, task checkboxes grouped Core / game / look / debug, spikes, and **written exit
  criteria**. Every phase ends playable. Tick boxes as work lands.
- Infrastructure early: localization keys, input parity, the wiki, saves, and budgets go in before the content that
  uses them.

## 7. Spikes

- A spike answers one question with numbers, in a time box (one or two days), and ends in an ADR with the
  measurements and the decision. List them in the plan's spikes table with the phase they block.
- Typical spikes: the renderer on minimum-spec hardware; the collision approach at the expected entity count; the
  post-processing approach; streaming and origin rebasing for a continuous world; the save model; an import pipeline
  for the art style; adaptive music (Godot's streams or middleware); aiming or camera feel (playtest-driven, recorded
  in the GDD).
- Leave the measurement harness in the repo (a dev flag such as `--benchmark`), so the numbers can be re-measured on
  real hardware later.

## 8. ADR habits

- One decision per file, `NNNN-kebab-title.md`, Context / Decision / Consequences, under a page. The title states the
  decision as a sentence.
- Accepted ADRs are immutable: supersede with a new ADR, and edit only the old one's status line (and the index).
- Write one for every new dependency, folder, autoload, project, renderer or physics choice, save format, and spike.
- The template ships ADRs 0001–0010 for its own decisions; number the game's first own decision 0011.

## 9. CLAUDE.md

- It's what every future session reads first: keep it accurate and short. The status section is three to five lines
  (current phase, what just landed, what's next).
- Commands must be copy-pasteable and verified. Record the machine-specific `GODOT_BIN` path.
- Add the game's dev flags as they appear (they're also on the wiki's dev-flags page).
- When a rule changes, change it in `docs/CODING_STANDARDS.md` and the CLAUDE.md summary in the same change.
