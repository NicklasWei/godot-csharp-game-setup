# 0006: Godot-runtime tests run in an in-repo headless runner, in CI

**Status:** Accepted (__DATE__)

## Context

Core tests can't catch engine-side failures: broken scenes, missing exports, shader compile errors, exceptions in
node scripts, or allocations in node code. Third-party Godot test frameworks tend to lag behind new Godot and .NET
versions and need packages inside the game project.

## Decision

- **Runner:** an in-repo runner in `tests/godot/runner` (about 400 lines, no packages). Tests are `public static`
  methods marked `[GodotTest]` under `tests/godot/` (namespace `__GAME__.RuntimeTests.*`), taking an optional
  `GodotTestContext` (a fresh container node in the live tree, plus frame waits) and returning `void` or `Task`.
  They compile into Debug builds only; `ExportRelease` builds exclude `tests/godot/**`, and CI builds both.
- **Failures:** a test fails when it throws, times out, or when Godot logs an engine, script, or shader error during
  it or in the frame after, captured with a C# `Logger` registered through `OS.AddLogger` (Godot 4.5+). The runner
  quits with exit code 1 on any failure, or when it finds no tests.
- **Allocations:** the runner awaits a test once against a single timer rather than polling per frame, so it adds no
  allocations while a test measures them. `AllocationProbe` counts main-thread bytes from inside the physics loop.
- **CI (`godot-smoke`):** downloads the exact Godot .NET release for Linux, builds, adds a `.gdignore` to the
  in-project NuGet cache, runs `--import` and a short headless boot, runs the runtime tests with `--junit`, and fails
  on any log line starting with `ERROR`, `SCRIPT ERROR`, or `USER ERROR` (with an explicit `exit 1`: under `set -e`, a
  `! grep` line can never fail the job).
- Arguments: `--filter=TEXT` selects tests, `--junit=PATH` writes a report.

## Consequences

- The 0 B/frame budget is checked in CI, not only by eye. In the project this template comes from, the runner's
  first run found a per-kill string allocation the debug overlay's sampling had missed.
- The runner is ours to maintain and has no IDE test-explorer integration; the log and the JUnit report are the
  interface.
- When gameplay exists, CI also runs it headless on the autopilot (`-- --autopilot`).
