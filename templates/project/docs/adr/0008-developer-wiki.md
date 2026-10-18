# 0008: A developer wiki generated from the code, built with DocFX

**Status:** Accepted (__DATE__)

## Context

Developers need one place to look up content, tuning, controls, vocabulary, and the design docs, with the real
numbers. A hand-written wiki goes stale as soon as content or tuning changes. A wiki added late, on a side branch, also
drifts from the code it describes; it has to grow with the code from the first commit.

## Decision

- **A generator project, `tools/__GAME__.Wiki`**, a console app in the solution that references Core and builds with
  the repo's analyzers. It reads the same objects the game reads, so the wiki can't disagree with the game:
  - content through `ContentLoader`, for every kind in `ContentKinds.All` (fields from the document types, every
    definition rendered by reflection);
  - every public `…Settings` record and its static instances, by reflection, for code-held tuning;
  - every public Core enum, as the glossary;
  - the input map from `project.godot`, with `InputActionNames` descriptions;
  - the dev flags from `DevFlags`;
  - the PO catalogs, for player-facing names and the localization status;
  - descriptions from Core's XML docs, which the build already requires on every member.
- **Parity by construction:** every page derives from a registry in code, so a new kind, field, settings record, enum,
  action, flag, or key appears without a generator change. A new registry gets a page writer in the same change.
- **DocFX** renders the site: a pinned .NET tool, so `dotnet tool restore` covers it and the repo needs no Python or
  Node. It also reads `docs/**/*.md` and `CLAUDE.md` in place, with a generated TOC, and builds the Core API reference.
- **Nothing generated is committed:** the pages go to `tools/wiki/generated/`, the site to `tools/wiki/_site/`, both
  gitignored. `tools/wiki/build.ps1` / `build.sh` build both; `-Serve` / `--serve` serves the site locally.
- **CI:** the `wiki` job runs with `--strict` (a content error, a missing English string, a missing XML summary, or a
  registry mismatch fails it) and DocFX `--warningsAsErrors` (a broken link fails it). The `pages` job publishes from
  the default branch; access is limited to project members if the project is private.
- **Isolation:** the game never references the generator; `tools/` is excluded from the game's compile glob and
  hidden from Godot by `tools/.gdignore`.

## Consequences

- Changing a content value, a PO string, a settings default, an action, or a doc comment changes the wiki on the next
  build, with no wiki edit. To change a page, change its source.
- A kind with special presentation needs (cross-references, computed tables) can get its own page writer on top of
  the generic one.
- Bumping DocFX means editing the tool manifest.
