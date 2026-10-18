## What and why

<!-- One or two sentences. Link the implementation plan task or ADR if relevant. -->

## How to verify

<!-- Commands, scenes to open, or what to look for in-game. -->

## Definition of Done (docs/CODING_STANDARDS.md §15)

- [ ] `dotnet csharpier check .` passes
- [ ] `dotnet build __GAME__.sln` succeeds with zero warnings
- [ ] All tests pass (Core, plus Godot runtime tests where relevant)
- [ ] New or changed Core logic has unit tests; bug fixes have a regression test
- [ ] XML docs on every new type and member; header block on every new shader
- [ ] No hardcoded user-facing strings; new keys in `en.po` and in other locales as `#, fuzzy`
- [ ] No new per-frame allocations in gameplay paths
- [ ] Content validates; no IDs renamed
- [ ] The wiki builds with `--strict`
- [ ] Docs updated if design, look, or architecture changed (GDD, art direction, standards, ADR)
- [ ] Implementation plan checkboxes updated
