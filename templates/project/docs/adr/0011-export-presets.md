# 0011: Export presets for Windows and Linux are committed, and just exports them

**Status:** Accepted (__DATE__)

## Context

- **Builds:** without presets, the game runs only from the editor and `just boot`, and the first export comes late,
  when nobody remembers what the pack must hold. A release export also proves that Core ships with the game.
- **.NET exports:** Godot builds the game assembly with `dotnet publish` in the `ExportRelease` configuration, which
  leaves the runtime tests out ([ADR 0006](0006-godot-runtime-tests-and-ci.md)), and writes the assemblies, Core's
  among them, into a `data_<assembly>_<platform>_<arch>/` folder beside the executable. A full export needs the
  editor version's .NET export templates, installed per machine; a pack-only export (`--export-pack`) doesn't.
- **What the pack must hold:** `ContentFiles` reads `res://content` through Godot's file API, so the content JSON has
  to be in the pack. Godot 4 counts every `.json` file as a resource (`JSON`), so "all resources" packs the content,
  and also the root `global.json` and `stylecop.json`. Folders with a `.gdignore` and hidden folders (`.config`,
  `.github`) are never scanned, but `dev/` and `tests/godot/` are, and would ship.

## Decision

- **`export_presets.cfg` is committed** with two presets, named without spaces so the command line, the justfile,
  and later CI need no quoting:
  - `Windows` (Windows Desktop, x86_64), exporting to `export/windows/__GAME__.exe`;
  - `Linux` (Linux, x86_64), exporting to `export/linux/__GAME__.x86_64`.
- **Filters:** all resources, with `content/*` included by name as well, so the content doesn't depend on how Godot
  classifies `.json`. Excluded: `dev/*` (look-dev and sandbox scenes), `tests/*` (the runtime tests' scenes and
  scripts), `content/schemas/*` (editor-only, and `ContentFiles` skips them), `global.json`, and `stylecop.json`.
- **Options** stay at Godot's defaults: the pack and the assemblies beside the executable rather than embedded, no C#
  sources, debug symbols included (line numbers in stack traces), and a console wrapper on Windows debug exports
  only. The file sets those explicitly; the editor fills in the rest when it saves the presets.
- **Recipes:** `just export-windows`, `just export-linux`, and `just export` (both) run `--headless --export-release`
  into the git-ignored `export/`, creating the folder first, since Godot refuses to export into a missing one. They
  aren't part of `just ci`.
- **Templates:** each machine installs the editor version's .NET export templates (Editor → Manage Export
  Templates, or the release's `_mono_export_templates.tpz`), and a Godot bump needs the new ones.

## Consequences

- Phase 0's export check is one command: `just export`, then the Core assembly is in the data folder, and the
  export boots and loads its content.
- Saving the presets in the editor's Export dialog rewrites the file with every option; review that diff and keep
  the filters and names.
- A new top-level file or folder that Godot can see (JSON at the root, a sandbox folder without `.gdignore`) needs a
  look at the exclude filter, and so does every preset added later.
- Other platforms (macOS needs a bundle identifier and signing; consoles need their own SDKs) get their presets in
  their own ADR, with the same filters.
- CI export jobs (on tags) come later, run these presets, and need the export templates in the CI cache.
