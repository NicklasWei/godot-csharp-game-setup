# __TITLE__ — Game Design Document

> **Status:** Living document, pre-production.
> All numbers marked *(tunable)* are starting values for playtesting, not commitments.
> Related: [Art Direction](ART_DIRECTION.md) · [Audio Direction](AUDIO.md) · [Coding Standards](CODING_STANDARDS.md) · [Implementation Plan](IMPLEMENTATION_PLAN.md)

<!--
How to fill this in: write what the game IS, in short declarative sentences. Every system gets numbers (marked
tunable), every name goes into the lexicon first, and every open decision goes into §17 rather than staying vague in
the text. Delete sections the game doesn't need (never Audio: a game without music or voice says so); add
game-specific ones after §8.
-->

---

## 1. Overview

| | |
|---|---|
| **Working title** | __TITLE__ |
| **Genre** | <!-- e.g. 2D platformer, turn-based tactics, 3D action roguelite --> |
| **Inspirations** | <!-- 3–6 games, each with what is taken from it --> |
| **Platforms** | <!-- e.g. Windows, Linux. Name the minimum-spec target hardware. --> |
| **Input** | <!-- e.g. controller-first, full mouse and keyboard parity; or mouse-first --> |
| **Music and sound** | <!-- music genre and style, and the sound-effect style, in a line each (Audio Direction §4, §5) --> |
| **Voice** | <!-- the voice model (full, partial, barks, efforts only, gibberish, sound-effect speech, synthesized, or none) and the voice languages (Audio Direction §7) --> |
| **Engine** | Godot (.NET) with C# |
| **Players** | <!-- single-player / local co-op / online --> |
| **Session length** | <!-- a typical play session or run, in minutes (tunable) --> |
| **Languages** | __LOCALES__ |

### 1.1 Pitch

<!-- One paragraph: who you are, what you do, why it's fun. -->

### 1.2 Design pillars

<!-- 3–5 pillars. Each is a sentence that can decide a design argument. -->

1. …

### 1.3 Non-goals

<!-- What this game deliberately is not, so scope stays honest. -->

---

## 2. Setting and fantasy

<!-- The world, the player's role, the tone. Keep it short; the lexicon carries the names. -->

---

## 3. Lexicon

These terms are binding. Use them in code identifiers, localization keys, UI, and docs. Don't use synonyms.

| Concept | Term | Code identifier | Notes |
|---|---|---|---|
| <!-- e.g. Health --> | <!-- the in-world word --> | <!-- PascalCase identifier --> | |

---

## 4. Game loops

### 4.1 Moment to moment (seconds)

### 4.2 Encounter or level (minutes)

### 4.3 Session or run

### 4.4 Meta (between sessions)

---

## 5. The player

<!-- The player's verbs: movement, actions, abilities, resources, with numbers (tunable). -->

---

## 6. Controls

Every action has a keyboard/mouse and a controller binding. The action names are the input map's (`InputActionNames`), and the generated wiki lists the bindings.

| Action | Keyboard and mouse | Controller | Notes |
|---|---|---|---|
| Pause | Escape | Start | |

---

## 7. Structure

<!-- How a session is laid out: levels, world, map, progression through it, transitions between parts. State here
whether transitions are scene swaps or one continuous world (see Coding Standards §3.9). -->

---

## 8. Systems

<!-- One subsection per system (combat, economy, crafting, dialogue, …): rules, formulas, numbers, edge cases. Rules
written here become Core code and tests. -->

---

## 9. Challenges and opposition

<!-- Enemies, hazards, puzzles, or opponents, and the readability rules that make them fair. -->

---

## 10. Content

<!-- The content kinds (items, levels, enemies, …): what each holds and its ID prefix. These become content kinds
in Core (ContentKinds.All) and pages in the wiki. -->

---

## 11. Progression and economy

---

## 12. Difficulty and fairness

---

## 13. UI and HUD

---

## 14. Audio

The full audio direction is in [Audio Direction](AUDIO.md): music, sound effects, ambience, voice, UI sound, the mix,
and the cue list.

<!-- Summarize the binding points in a few bullets: the music's style and how it adapts, the sound-effect style, the
voice model and its languages, the buses, the loudness target, and every audio rule that affects play or fairness
(e.g. every telegraph is both seen and heard). AUDIO.md owns the detail; keep the two in step. -->

---

## 15. Accessibility

<!-- Remapping, subtitles, colorblind support, motion and flash intensity, difficulty assists, text size. -->

---

## 16. Content targets

<!-- MVP and full-release counts for each content kind. -->

| Content | MVP | Release |
|---|---|---|

---

## 17. Open questions

<!-- Each open decision, with who decides and by when (a phase or spike). -->
