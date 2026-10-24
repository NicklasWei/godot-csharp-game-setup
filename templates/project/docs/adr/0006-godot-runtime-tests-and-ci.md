# 0006: Godot-runtime tests run in an in-repo headless runner, in CI

**Status:** Accepted (__DATE__)

## Context

Core tests can't catch engine-side failures: broken scenes, missing exports, shader compile errors, exceptions in
node scripts, or allocations in node code. Third-party Godot test frameworks tend to lag behind new Godot and .NET
versions and need packages inside the game project.

Godot's C# glue also has a race that a test suite walks into
([godot#112067](https://github.com/godotengine/godot/issues/112067), open as of 4.8-dev7). A `RefCounted` object
whose only owner is its C# wrapper is freed by the wrapper's finalizer, on the finalizer thread, some time after the
GC collects the wrapper. If native code references the object in between, as a load does when the resource cache
still has it, Godot leaves its instance binding with a released handle: its next reference logs `Handle is not
initialized` from `SwapGCHandleForType`, and its free logs the same from `SetGodotObjectPtr`. A suite that loads a
scene from C# and drops it leaves exactly that behind: the scene, then the materials and sub-scenes that views hold
in `[Export]` fields, are freed by finalizers whenever the GC gets to them, while a later test may load them again.
The result is a rare error in whichever test happens to be running, which looks like a flaky test.

## Decision

- **Runner:** an in-repo runner in `tests/godot/runner` (about 400 lines, no packages). Tests are `public static`
  methods marked `[GodotTest]` under `tests/godot/` (namespace `__GAME__.RuntimeTests.*`), taking an optional
  `GodotTestContext` (a fresh container node in the live tree, plus frame waits) and returning `void` or `Task`.
  They compile into Debug builds only; `ExportRelease` builds exclude `tests/godot/**`, and CI builds both.
- **Failures:** a test fails when it throws, times out, or when Godot logs an engine, script, or shader error during
  it or in the frame after, captured with a C# `Logger` registered through `OS.AddLogger` (Godot 4.5+). The runner
  quits with exit code 1 on any failure, or when it finds no tests.
- **Scenes and finalizers:** tests load scenes through `GodotTestContext.Scene` and `Instantiate`, never
  `ResourceLoader`. The runner loads each scene once and holds it until the run ends (`LoadedScenes`), as `Main` holds
  its scenes for a session, so nothing a scene uses is freed and reloaded mid-run. After each test, it runs
  `GC.Collect()` and `GC.WaitForPendingFinalizers()` while the main thread waits, repeating while a round frees Godot
  objects, so no wrapper a test left is finalized during a later test, and a finalizer's errors count against the
  test that left it. At the end it releases the scenes and settles again; errors there fail a
  `GodotTestRunner.Teardown` result. `--gc-stress` holds the race open instead: it parks the finalizer thread
  (`FinalizerGate`) and lets it run only between tests, a step at a time, so whatever a test leaves waits collected
  but unfinalized through the next test, and a race that a normal run hits only rarely shows up there.
- **Allocations:** the runner awaits a test once against a single timer rather than polling per frame, so it adds no
  allocations while a test measures them. `AllocationProbe` counts main-thread bytes from inside the physics loop.
- **CI (`godot-smoke`):** downloads the exact Godot .NET release for Linux, builds, adds a `.gdignore` to the
  in-project NuGet cache, runs `--import` and a short headless boot, runs the runtime tests with `--junit`, and fails
  on any log line starting with `ERROR`, `SCRIPT ERROR`, or `USER ERROR` (with an explicit `exit 1`: under `set -e`, a
  `! grep` line can never fail the job).
- Arguments: `--filter=TEXT` selects tests, `--junit=PATH` writes a report, and `--gc-stress` parks the finalizer
  thread between tests.

## Consequences

- The 0 B/frame budget is checked in CI, not only by eye. In the project this template comes from, the runner's
  first run found a per-kill string allocation the debug overlay's sampling had missed.
- The runner is ours to maintain and has no IDE test-explorer integration; the log and the JUnit report are the
  interface.
- When gameplay exists, CI also runs it headless on the autopilot (`-- --autopilot`).
- Game code follows the same rule as the tests: hold a resource loaded by path for as long as anything may load it
  again (Standards §5.8). Run the suite with `--gc-stress` after changing how tests or views hold resources, and when
  upgrading Godot. Once a Godot release fixes godot#112067, the settling between tests can go, and `--gc-stress` is
  the check that it's fixed.
