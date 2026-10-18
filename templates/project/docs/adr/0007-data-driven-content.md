# 0007: Content is JSON in registered kinds, with permanent IDs and generated schemas

**Status:** Accepted (__DATE__)

## Context

Tunable numbers and definitions (items, enemies, levels, tables) change constantly during development and must be
validated, versioned, diffable, readable by Core tests and tools without the engine, and referenced by saves.

## Decision

- Content is JSON in `content/<kind>/<name>.json`, parsed by Core with System.Text.Json **source generation**
  (`ContentJsonContext`): comments and trailing commas allowed, **unknown properties rejected**, camelCase names.
- **Kinds are registered once** in `ContentKinds.All`: folder, ID prefix, document type, validator, and required
  localization keys. The loader, the schema and localization tests, and the wiki all read that list.
- **IDs** are `<prefix>.<file name>` in snake_case, enforced by the loader, and **permanent**: never renamed or reused.
- The loader collects every problem with its file path, leaves broken files out, and the game logs them all at boot.
  The game reads files through Godot's file API, which works in exports.
- **Schemas** in `content/schemas/` are generated from the document types with `JsonSchemaExporter`, and a test fails
  when one is stale. Each content file declares its `$schema`, so editors validate and autocomplete.
- Numbers not yet in content live in public `…Settings` records with named static instances, so the wiki still shows
  them.

## Consequences

- Adding a kind is a document, a definition, a validator, one registration, and a schema regeneration.
- Content can't silently drift from the code: unknown fields, stale schemas, bad IDs, and missing strings fail tests.
- Godot `Resource`s stay for editor-authored visual and scene data only.
