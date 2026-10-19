# 0010: Project commands run through a justfile

**Status:** Accepted (__DATE__)

## Context

Every gate is a long command (the coverage run alone has seven options), the Godot commands need a per-machine
`GODOT_BIN` path, and the wiki and hook scripts come in a PowerShell and a shell variant. Developers and Claude
sessions retype or copy them from CLAUDE.md. The options considered: just, Task (`Taskfile.yml`), a .NET 10
file-based script (`dotnet run tasks.cs`), Nuke or Cake, paired `.ps1`/`.sh` scripts, npm scripts, and Make.

## Decision

- A root `justfile` holds the routine commands as short recipes (`just build`, `just test`, `just cover`,
  `just godot-test`, `just wiki`, `just ci`, …), each with a one-line comment that `just --list` shows.
- Recipes run in PowerShell on Windows (`windows-shell`) and `sh` elsewhere; the few that differ per OS use the
  `[windows]` / `[unix]` attributes.
- `set dotenv-load` reads machine-specific settings from a git-ignored `.env`; `.env.example` documents them.
  Godot recipes fail with a hint when `GODOT_BIN` is missing.
- `just ci` runs every gate in CI order. CI and the pre-commit hook keep calling the tools directly.

## Consequences

- One extra tool per developer machine (a single binary from winget, brew, apt or cargo).
- CLAUDE.md and the README lead with recipe names; the raw commands stay documented for machines without `just`.
- A new routine command gets a recipe in the same change that introduces it.
