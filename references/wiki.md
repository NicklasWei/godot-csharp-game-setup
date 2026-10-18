# The generated developer wiki

The template's wiki is generated from the code, so it has full parity with it by construction: every page derives
from a registry in code, CI rebuilds it on every pipeline with `--strict`, and nothing generated is committed.
ADR 0008 in the template records the decision; this file is how it works and how to extend it.

## Contents

1. What's in it and where each page comes from
2. How parity is enforced
3. Build, serve, publish
4. Extending it
5. Troubleshooting

---

## 1. What's in it and where each page comes from

| Section | Page | Source of truth |
|---|---|---|
| Content | Overview | `ContentKinds.All`: each kind's folder, ID prefix, definition count, schema |
| Content | One page per kind | The kind's document type (fields, JSON names and types, XML summaries), every definition rendered by reflection, its localization keys with English text |
| Reference | Tuning | Every public Core type named `…Settings` and its static instances (`Default`, named presets), rendered by reflection |
| Reference | Glossary | Every public Core enum: members, camelCase content names, XML summaries |
| Reference | Controls | `InputActionNames.All` with summaries, bindings parsed from `project.godot` |
| Reference | Dev flags | `DevFlags` properties paired with their `…Argument` / `…Prefix` constants |
| Reference | Localization | Every PO catalog: per-locale status and every key's English text and state |
| Design docs | GDD, art direction, standards, plan, ADRs | `docs/**/*.md`, read in place, TOC generated |
| Core API | Every public Core type | The Core assembly and its XML docs (DocFX metadata) |
| Project overview | CLAUDE.md | Read in place |

Descriptions are always the XML doc summaries the build already requires, so documenting code well is documenting the
wiki. Property summaries lose their "Gets …" prefix on the way.

## 2. How parity is enforced

- **By construction:** pages iterate registries (`ContentKinds.All`, exported types, `InputActionNames.All`,
  `DevFlags` members, the PO files). A new kind, field, settings record, enum, action, flag, key, or doc appears with no
  generator change.
- **Strict mode** (`--strict`, on in CI) turns these into failures: a content load error; a missing English string for
  a key a page shows; a Core property, field, constant, or enum member without an XML summary; an input action in
  `project.godot` but not in `InputActionNames` (or the reverse); a dev flag without its argument constant (or the
  reverse).
- **DocFX `--warningsAsErrors`** fails on broken links and bookmarks in the docs.
- **The rule for humans and Claude:** a new registry (console commands, achievements, a save schema, audio cues) gets
  a page writer in the same change that adds it.

## 3. Build, serve, publish

```bash
dotnet tool restore
```
```bash
tools/wiki/build.sh --strict --serve
```

(`tools/wiki/build.ps1 -Strict -Serve` on Windows.) The script builds the generator, writes the pages to
`tools/wiki/generated/`, and runs DocFX into `tools/wiki/_site/`; `--serve` serves it at `http://localhost:8080`.
In the Claude desktop app, `.claude/launch.json` has a `wiki` configuration to preview a built site.

CI: the `wiki` job runs the generator with `--strict`, the commit, and a source URL (so pages link to files on the
code host), then DocFX; the `pages` job publishes from the default branch. For a private project, limit Pages to
project members (GitLab: Settings > General > Visibility; GitHub: private repository Pages visibility).

## 4. Extending it

**A new page:** add a static class in `tools/<Game>.Wiki/Pages/` with
`public static IEnumerable<GeneratedPage> Write(WikiContext context)`, and add it to `WikiGenerator.Writers` in
navigation order. Use `context.StartPage(title, sources…)` (it adds the "generated from … at commit …" note),
`ObjectTable.Write` to render any object by reflection, `context.Describe(member)` for summaries (it reports missing
ones), `context.Text(key)` for English strings (it reports missing ones), and `context.Report(problem)` for any
parity mismatch strict mode should fail on.

**A richer page for one content kind:** keep the generic kind page and add a section or a separate page with
computed cross-references (which levels use an enemy, which items share a tag).

**Presentation rules:** `ObjectTable` shows `…Radians` floats in degrees; `ValueFormatter` formats numbers in the
invariant culture, enums by their camelCase content names, and vectors as tuples. Add a rule there rather than
special-casing a page.

**Game-layer data** (scenes, node exports) isn't in the wiki by default, since the generator references Core only.
If something there must be documented, move its data into Core or content first; that's usually the right design
anyway.

## 5. Troubleshooting

- *"the Core XML docs are missing"*: build the generator project first (`dotnet build tools/<Game>.Wiki`); the Core
  XML file is copied next to the Core DLL in its output.
- *The output folder holds files the generator didn't write*: it refuses to clear a folder without its marker file.
  Delete `tools/wiki/generated/` by hand once.
- *A TOC link doesn't resolve*: DocFX resolves TOC links from the TOC file's own location; generated TOCs point at
  sources relative to `tools/wiki/generated/<section>/`.
- *A broken bookmark into a doc*: DocFX anchors are the heading text lowercased with spaces as hyphens (`## 3. Lexicon`
  is `#3-lexicon`).
- *DocFX version bumps*: edit `.config/dotnet-tools.json` (pinned with `rollForward: false`) and rebuild with
  `--warningsAsErrors`.
