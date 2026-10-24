# __TITLE__ — Audio Direction & Sound Design Bible

> **Status:** Living document, pre-production.
> Related: [GDD](GDD.md) · [Art Direction](ART_DIRECTION.md) · [Coding Standards](CODING_STANDARDS.md) · [Implementation Plan](IMPLEMENTATION_PLAN.md)

<!--
How to fill this in: decide how the game sounds with references first (§1, §3), then turn it into rules and lists a
composer, a sound designer, or a voice director can follow. Fill §1–§8 and §10.1 in Phase 0, even when the answer is
"no music" or "no voice acting": an explicit "none" is a decision, a blank is not. Mark anything undecided *(open)*,
with the phase that decides it, and list it in §16. Terms are explained in §17. The GDD keeps a short summary in its
Audio section; this document owns the detail.
-->

---

## 1. Goals and keywords

<!-- A one-line target for how the game sounds, 3–5 keywords, and what audio must achieve for play: readability
(can the player hear what matters?), feel (does an action sound as strong as it is?), and mood. -->

**One-line target:** *…*

| Keyword | Meaning |
|---|---|
| | |

---

## 2. Rules

These rules are binding and checked in the audio QA (§15). Keep, change, or delete the defaults; add the game's own.

1. **Every gameplay-critical cue is both seen and heard.** A warning, a threat, or a hit never lives in sound alone (deaf and hard-of-hearing players, muted speakers) or in visuals alone (off-screen events).
2. **The most repeated sounds never fatigue.** Whatever plays hundreds of times a session (footsteps, the main attack, UI focus) is short, soft, and varied, and a cue plays at most once per tick however many events of its kind arrive.
3. <!-- e.g. "Harmless things are quiet": contact that does no damage makes little or no sound. -->

---

## 3. References

<!-- Music, sound, and games or films, each with what to take from it: tempo, instrumentation, mood, a mix trait, a
signature sound. Name real tracks or albums for music; "dark synth" means different things to different people.
References are for mood and principles, never for copying, and temp tracks used in prototypes never ship. -->

| Reference | Kind | What to take from it |
|---|---|---|
| | <!-- music / SFX / game / film --> | |

---

## 4. Music

### 4.1 Style and genre

| | |
|---|---|
| **Genre and style** | <!-- e.g. orchestral, darksynth, lo-fi hip hop, chiptune, folk; or "no music" --> |
| **Mood** | <!-- how it should make the player feel, and how that changes over the game --> |
| **Instrumentation** | <!-- the instruments or synths that define it --> |
| **Tempo and key** | <!-- e.g. 90–120 BPM, mostly minor keys (tunable) --> |
| **Source** | <!-- an original score (composer?), licensed tracks, a royalty-free library, procedural, or a mix --> |
| **In-world music** | <!-- diegetic music (radios, performers, a jukebox), or none --> |

### 4.2 Where music plays

<!-- Each situation and its track or style: title and menus, exploration, combat, bosses, victory, defeat, credits.
Per area, level, or act if the music changes with them. -->

| Situation | Music |
|---|---|
| | |

### 4.3 Adaptive behavior

<!-- Linear (a track plays and loops) or adaptive. Adaptive music uses vertical layering (stems that play in lockstep,
faded in and out with intensity), horizontal resequencing (sections or loops that switch on a beat or bar), stingers
for events, or a mix. Say which game state drives it (combat intensity, danger, location, health) and how changes
land (crossfade length, on the next bar). In Godot (4.3 and later): AudioStreamSynchronized plays layers in
lockstep, AudioStreamInteractive switches clips through a transition table, AudioStreamPlaylist plays a sequence, and
they nest. Does the beat drive anything visual? -->

### 4.4 Transitions, menus, and pause

<!-- What the music does on a scene change (or through a continuous world), under in-game menus (duck, filter, keep
playing), on pause, on death, and on victory. -->

---

## 5. Sound effects

### 5.1 Style

<!-- Realistic recorded sound (Foley), stylized, synthesized, retro or chiptune, cartoony, or a mix; and where sounds
come from: recorded, from a library (with its license), synthesized, or commissioned. -->

### 5.2 Categories and signature sounds

<!-- The sounds that define the game, by category. A signature sound is one the player will remember and recognize. -->

| Category | Sounds | Notes |
|---|---|---|
| Player actions | <!-- movement, attacks, abilities --> | |
| Feedback | <!-- hits landed, damage taken, pickups, rewards --> | |
| Threats | <!-- enemy attacks, telegraphs, hazards --> | |
| World | <!-- interactables, machines, doors --> | |
| Big moments | <!-- victory, defeat, boss phases, level complete --> | |

### 5.3 Cue list

<!-- Every gameplay event and the cue it plays. It grows with the game; the audio phase's exit ("every event has
sound") is checked against it. Core's per-tick session events are the natural keys. -->

| Event | Cue | Bus | Notes |
|---|---|---|---|
| | | | |

---

## 6. Ambience

<!-- What the world sounds like between events: beds per area (wind, machinery, room tone, crowd walla), spot sounds
placed in the world (a humming generator, a waterfall), reverb per space, and how ambience changes with time,
weather, or tension. "None" is an answer. -->

| Area | Bed | Spot sounds | Reverb |
|---|---|---|---|
| | | | |

---

## 7. Voice and dialogue

<!-- Decide this in Phase 0: the voice model drives the budget, the localization plan, subtitles, line management,
casting, and recording. Changing it late means re-scripting, re-recording, or both. -->

### 7.1 The voice model

**Chosen:** <!-- one model from the table, or a mix, e.g. "barks and efforts in gameplay, text with blips in dialogue" -->

| Model | What it is | Examples | What it brings with it |
|---|---|---|---|
| **Full voice acting** | Actors speak all dialogue | Most story-driven action games | A script and line database, casting, recording sessions, lip sync if faces show, subtitles, and the biggest localization cost |
| **Partial voice acting** | Key scenes or lines are voiced; the rest is text | Many RPGs | The same pipeline at a smaller scale, plus a rule for what gets voiced |
| **Barks only** | Short reactive lines during play ("Reloading!", "Over there!"); dialogue is text | Many shooters and tactics games | Many short lines with variants, and trigger and repetition rules |
| **Efforts only** | Wordless vocal sounds: grunts, breaths, pain, death, laughs | Many platformers and action games | A short session per character and no translation; effort sessions strain voices, so plan them short and last |
| **Gibberish** | Speech-like sounds in no real language, performed or generated | The Sims (Simlish), Banjo-Kazooie, Animal Crossing | One recording for every locale; emotion without words, with text carrying the meaning |
| **Sound-effect speech** | Beeps, boops, or chirps per letter or word as text appears, pitched per character | Undertale, Ace Attorney | UI cues only; no recording or localization cost |
| **Synthesized voice** | Text-to-speech or a processed synthetic voice, often an in-world machine | In-world AIs and robots | Generated per locale or in one language; check the tool's license for commercial use |
| **None** | No voice; all dialogue is text | Many indie games | Nothing beyond the PO catalogs |

| Question | Answer |
|---|---|
| **Who speaks** | <!-- characters, a narrator, an announcer, the UI --> |
| **Line count** | <!-- rough MVP and release counts; barks count per variant --> |
| **Performance style** | <!-- naturalistic, theatrical, processed or filtered (radio, robot) --> |
| **Casting and recording** | <!-- who performs, studio or remote, union terms, schedule --> |
| **Lip sync** | <!-- none, jaw flap, or phoneme-based; only if faces show --> |
| **Subtitles and captions** | <!-- on by default? speaker names? sound captions (§11)? --> |

### 7.2 Voice localization

**Chosen:** <!-- one row; the game's text languages are __LOCALES__ -->

| Approach | What it means | What it costs |
|---|---|---|
| **Nothing to localize** | No voice, or only efforts, gibberish, or sound-effect speech | Nothing beyond the text |
| **One voice language, subtitled** | Recorded in one language; every locale gets subtitles | One recording; subtitles are required |
| **Dubbed in selected languages** | Re-recorded in the biggest markets (often French, Italian, German, and Spanish, "FIGS"), subtitled elsewhere | Casting and sessions per language, and an audio-language setting separate from the text language |
| **Fully dubbed** | Every locale gets its own recording | The highest cost: every line change is re-recorded in every language |

<!-- If the game is voiced: key each line by the PO key of its subtitle (voice/<locale>/<key>.ogg, for example), so a
missing recording can fail a file-based test the way a missing translation does. -->

---

## 8. UI sound

<!-- Focus, confirm, back, error, open, close, typing: consistent with the SFX style, quiet enough to hear hundreds
of times, and distinct enough to tell apart with eyes closed. "None" is an answer. -->

---

## 9. Spatial audio

<!-- 2D or 3D sound; the listener (the camera or the player); which cues are positional and which aren't; attenuation
(distance curves, max distance), panning, doppler, occlusion, and reverb zones (Area2D/Area3D can route sound to a
reverb bus). -->

---

## 10. Mix

### 10.1 Buses

| Bus | Carries | Sends to |
|---|---|---|
| **Master** | Everything | Output |
| **Music** | The music | Master |
| **SFX** | Gameplay sound effects | Master |
| **Ambience** | Beds and spot sounds | Master |
| **UI** | Menu and HUD sounds | Master |
| **Voice** | Dialogue, barks, and efforts (only if the game has voice) | Master |

<!-- With the first sound, create game/audio/bus_layout.tres with these buses and make it the default bus layout
(Project Settings > Audio > Buses, stored in project.godot as [audio] buses/default_bus_layout). Each bus gets a volume
slider in the settings. Pin the bus list with a file-based test, like the input map's. -->

### 10.2 Mix hierarchy and ducking

<!-- What must always be heard (threats, telegraphs, dialogue), down to what yields (music, ambience). What ducks what:
dialogue ducks music; in-game menus duck or filter the game. -->

### 10.3 Loudness

<!-- A target integrated loudness and true peak, measured over a typical session. A common reference for console
games is -24 LUFS integrated (±2 LU) with a true peak at or below -1 dBTP (Sony's ASWG-R001); PC has no platform
standard, and mobile usually runs louder. Check the mix on the minimum-spec device's speakers and on headphones.
Offer a reduced dynamic range option ("night mode") if the game is very dynamic. -->

---

## 11. Accessibility

<!-- Subtitles (on by default?), captions for important sounds ([alarm], speaker names), visual indicators for
directional or off-screen sounds, a volume slider per bus, a mono downmix, and no information in sound alone (§2). -->

---

## 12. Implementation

- **Presentation follows Core.** Audio reacts to the session's events after each step and never changes Core state ([Coding Standards §3.5](CODING_STANDARDS.md#35-simulation-in-core-nodes-are-views)). Whether something is a threat, a hit, or a telegraph is a Core event; the audio layer only picks and plays a sound for it.
- **0 B/frame applies to audio** ([Coding Standards §8](CODING_STANDARDS.md#8-performance)). Give each cue one player and set its `max_polyphony` so overlapping plays ring out; never create or free players while playing. Bus names and other `StringName`s live in `static readonly` fields.
- **Variation is cosmetic randomness:** random pitch, volume, or sample choice uses the cosmetic stream, never a gameplay `RngStream` ([Coding Standards §3.6](CODING_STANDARDS.md#36-randomness-and-determinism)).
- **Headless runs mute audio.** There's nothing to hear, and Godot's dummy audio driver leaks any stream still playing at exit, which logs errors.
- **Godot's audio by default:** `AudioStreamPlayer`, `AudioStreamPlayer2D`, and `AudioStreamPlayer3D` on buses, plus the interactive, synchronized, and playlist streams for adaptive music. Middleware such as FMOD or Wwise is a Godot addon: ask first and record an ADR.
- **A registry of audio cues** (cues as content, for example) gets a wiki page writer in the same change.

---

## 13. Asset pipeline and placeholders

<!-- Formats: WAV for short effects (Godot can compress it on import), Ogg Vorbis for music and long beds (streamed).
Sample rate; mono for positional sounds, stereo for music and beds; loop points. Naming: snake_case files under
assets/audio/<music|sfx|ambience|ui|voice>/. Audio files are in Git LFS (.gitattributes); source projects (DAW
sessions) go in art-src/. Keep a credits list with the license of every library sound and licensed track.
Placeholder audio from Phase 1: a generator script (deterministic, so LFS copies change only with the script) or
licensed temp sounds, never shipped unlicensed. -->

---

## 14. Budgets

Budgets are for the minimum-spec target.

| Budget | Value |
| --- | --- |
| Simultaneous voices (sounds playing at once) | <!-- e.g. 32 --> |
| Audio memory | <!-- MB --> |
| Streamed tracks at once | |
| Audio CPU time | <!-- ms per frame --> |
| Managed allocations per gameplay frame | **0 B** |

---

## 15. Plan and QA

<!-- Phase 0: §1–§8 and the buses decided, even if "none". Phase 1: placeholder audio for the slice's events on the
buses. The audio phase: final assets, the mix, the loudness target, and the checklist below. -->

**Audio QA checklist:**

- [ ] Every event in the cue list (§5.3) has a sound.
- [ ] Every gameplay-critical cue can be told apart by ear, and is also shown (§2).
- [ ] The most repeated sounds don't tire the ear over a long session.
- [ ] Music transitions land musically, and nothing cuts off abruptly.
- [ ] Dialogue is intelligible over music and effects, and subtitles match it (if the game is voiced).
- [ ] The mix meets its loudness target with no clipping, on the minimum-spec device's speakers and on headphones.
- [ ] Each bus's volume slider works on its own.

---

## 16. Open questions

<!-- Each open audio decision, with who decides and by when (a phase or spike). -->

---

## 17. Glossary

| Term | Meaning |
|---|---|
| **Adaptive (interactive) music** | Music that changes with the game state instead of playing as a fixed recording |
| **Stem** | One group of instruments from a track, exported as its own file (drums, bass, pads) |
| **Vertical layering** | Adaptive music by stacking stems that play in lockstep and fading layers in and out |
| **Horizontal resequencing** | Adaptive music by switching between sections or loops over time, usually on a beat or bar |
| **Stinger** | A short musical hit played over or between sections for an event, or to hide a transition |
| **Temp track** | Existing music used as a placeholder or brief during development; never shipped without a license |
| **Cue** | One sound the game plays for one thing that happens |
| **One-shot / loop** | A sound that plays once / a sound that repeats seamlessly until stopped |
| **Foley** | Everyday sounds performed and recorded to match actions: footsteps, cloth, handling objects |
| **Bed** | A continuous background layer, such as a place's ambience |
| **Room tone** | The quiet background sound of a space, so silence doesn't sound dead |
| **Spot sound (emitter)** | A sound placed at a point in the world |
| **Diegetic / non-diegetic** | A sound that exists in the game world (a radio) / one only the player hears (the score, UI sounds) |
| **Bus** | A mixer channel that sounds are routed through, with its own volume and effects |
| **Ducking** | Temporarily lowering one bus (usually the music) so something else reads, often with a sidechain compressor |
| **Attenuation** | How a positional sound gets quieter (and duller) with distance |
| **Doppler** | The pitch rise and fall of a sound moving past the listener |
| **Occlusion** | Muffling a sound when something solid is between it and the listener |
| **Polyphony (voices)** | How many sounds play at the same time, per cue or in total |
| **LUFS** | Loudness Units relative to Full Scale: perceived loudness, measured over the whole mix (integrated) |
| **dBTP** | Decibels true peak: the highest level the waveform reaches, including between samples |
| **Voice over (VO)** | Recorded spoken lines |
| **Efforts** | Wordless vocal sounds: grunts, breaths, pain and death sounds |
| **Barks** | Short spoken lines a character says in reaction to what happens, outside any conversation |
| **Walla** | Unintelligible crowd chatter used as background |
| **Gibberish** | Speech made of invented syllables, so it sounds like talking without being any language |
| **Sound-effect speech (text blips)** | A beep or chirp per letter or word as text appears, standing in for a voice |
| **Lip sync** | Matching a character's mouth movement to the spoken line |
| **Dubbing** | Recording the voice again in each language, replacing the original |
| **Subtitles / captions** | On-screen text for spoken lines / subtitles that also name the speaker and describe important sounds |
