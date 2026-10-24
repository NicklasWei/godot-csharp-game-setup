# __TITLE__ — Art Direction & Tech-Art Bible

> **Status:** Living document, pre-production.
> Related: [GDD](GDD.md) · [Audio Direction](AUDIO.md) · [Coding Standards](CODING_STANDARDS.md) · [Implementation Plan](IMPLEMENTATION_PLAN.md)

<!--
How to fill this in: decide the look with references first (§1, §16), then turn it into rules a shader or an artist
can follow: palette roles, readability, budgets. Every rule here should be checkable in the look-dev scene.
-->

---

## 1. Goals and keywords

<!-- 3–6 keywords, and what the look must achieve for play (readability first). -->

## 2. Visual hierarchy

<!-- What the eye must find first, second, third: player, threats, interactables, background. -->

## 3. Readability rules

<!-- Shape, value, and color rules that make gameplay legible; these beat style. E.g. threats always use one
shape and one palette role. -->

## 4. Color system

Colors are **palette roles**: global shader uniforms (`pal_*`, in `project.godot`'s `[shader_globals]`), never raw colors in shaders or scenes. Palettes are content, so they can change per area.

| Role | Used for |
|---|---|
| <!-- pal_background --> | |

## 5. Shape language

## 6. Environment

## 7. Materials and shader library

<!-- The shared shaders and .gdshaderinc files, and which renderer features they rely on. Every shader has a header
comment and appears in the look-dev scene. -->

## 8. Post-processing

<!-- Effects, their budget, and how they scale with the accessibility intensity setting. Prefer one canvas uber
shader below the HUD; a C# CompositorEffect allocates every frame. -->

## 9. VFX

## 10. Lighting and rendering settings

<!-- Renderer (Forward+, Mobile, or Compatibility) and why; it is provisional until measured on minimum-spec
hardware. -->

## 11. Camera and motion feel

<!-- Field of view, follow behavior, shake (with a setting), and comfort rules. -->

## 12. UI art direction

<!-- Fonts (with CJK coverage if needed), layout that survives 35% text expansion, controller-first focus. -->

## 13. Asset pipeline

<!-- Source tools and formats, import rules (folders, naming, material slot names), Git LFS. Enforce rules in an
import script plus tests, not by memory. -->

## 14. Performance budgets

Budgets are for the minimum-spec target at its native resolution.

| Budget | Value |
| --- | --- |
| Frame rate | <!-- e.g. 60 fps --> |
| GPU frame time | <!-- ms --> |
| Main-thread CPU frame time | <!-- ms --> |
| Draw calls | |
| Triangles on screen | |
| Managed allocations per gameplay frame | **0 B** |
| Load time | |

A benchmark mode (a dev flag) measures these and is re-run at every phase exit; derive a dev-machine tripwire from the hardware ratio until the target hardware is available.

## 15. Look-dev and visual QA

<!-- The look-dev scene in dev/ shows every material and shader with camera presets and sliders; a test fails if one
is missing. Visual checks without a person use the movie writer (Coding Standards §9). -->

## 16. References
