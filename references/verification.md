# Phase 0 verification

Run these after scaffolding, in order, and report the results faithfully. A gate that has never been seen failing
might not be wired in, so step 4 breaks each one once on purpose. Every command runs from the repository root.

The scaffold was verified this way on Windows with .NET SDK 10.0.401 and `net10.0`, against Godot 4.5.1-stable,
4.6.1-stable, and 4.8-dev7: zero build warnings (Debug and ExportRelease), 90 Core tests passing at 98.5% Core line
coverage, a clean headless import and boot, 5 runtime tests passing (0 B/frame on the title screen), and the wiki
building with `--strict` and DocFX `--warningsAsErrors`; in GitLab, GitHub, English-only, and multi-locale variants,
and again after adding a sample content kind. The CI files were checked as valid YAML and mirror a pipeline that runs
green on GitLab, but neither was run on its host from the template: watch the first pipeline closely (step 5).

With [just](https://github.com/casey/just) installed and `GODOT_BIN` set in `.env` (copy `.env.example`), `just ci`
runs sections 1 to 3 in one go. The steps below are the same gates one at a time.

## 1. .NET gates

```bash
dotnet tool restore
```

```bash
dotnet csharpier check .
```

```bash
dotnet build <Game>.sln
```

```bash
dotnet build <Game>.csproj -c ExportRelease
```

```bash
dotnet test --project tests/<Game>.Core.Tests --coverlet --coverlet-output-format cobertura --coverlet-include "[<Game>.Core]*" --coverlet-threshold 85 --coverlet-threshold-type line
```

Expect zero warnings and all tests passing. The first build restores packages (Godot.NET.Sdk comes from nuget.org; no
editor needed).

## 2. Godot gates

Set `GODOT_BIN` to the **console** build of the editor matching the pinned version. Claude's shell doesn't inherit the
user's environment, so use the full path (quoted; it often contains spaces).

```bash
"$GODOT_BIN" --headless --path . --import
```

```bash
"$GODOT_BIN" --headless --path . --quit-after 120
```

```bash
"$GODOT_BIN" --headless --path . res://tests/godot/test_runner.tscn
```

The import creates `.uid` files next to the scripts: commit them. The boot must log no line starting with `ERROR`,
`SCRIPT ERROR`, or `USER ERROR`. The runner prints `PASS`/`FAIL` per test and exits 1 on any failure. A warning about
an unknown argument (`--junit=…`) from the main-scene test is expected: `Main` warns about arguments it doesn't know.

Open the project in the editor once and run it (F5): the title screen shows the game's name and the build version.

## 3. Wiki

```bash
sh tools/wiki/build.sh --strict
```

(`tools/wiki/build.ps1 -Strict` on Windows; both need a git repository for the commit hash.) Expect "Wrote 6 pages"
with no content kinds, and DocFX reporting 0 warnings and 0 errors. To look at it: add `--serve` and open
`http://localhost:8080`.

## 4. Prove each gate fails, then revert

Make each change, run the gate, confirm the failure, and undo the change:

| Break | Gate | Expected failure |
| --- | --- | --- |
| A public class in Core with no XML docs | build | CS1591 and SA1600 |
| `new System.Random()` in Core | build | RS0030 |
| `GD.Print("x")` in a game script | build | RS0030 |
| Bad formatting in any `.cs` file | `csharpier check` | exit 1, "Was not formatted" |
| Remove the controller binding from `pause` in `project.godot` | Core tests | `InputMap_Action_HasJoypadBinding` |
| A new key in `en.po` that nothing uses | Core tests | `EnglishCatalog_EveryKey_IsReferencedByCodeContentOrScenes` |
| A label with literal text `"Hello"` in a scene | Core tests | `Scenes_EveryTranslatableText_IsKnownKey` |
| A `private static int _count;` that a Core method increments | Core tests | `CoreTypes_HaveNoMutableStaticFields` (a public one already fails the build: CA2211, SA1401) |
| `GD.PushError("x")` in `TitleScreen._Ready` (suppress RS0030 for the probe) | runtime tests | every test showing the title fails with "Godot logged errors: x" |

The wiki's strict checks overlap the tests on purpose (content errors, missing strings, missing docs, registry
mismatches): the build or the tests usually fail first, and `--strict` catches whatever slips past them.

Coverage: temporarily raise `--coverlet-threshold` to a whole number above the reported rate (for example `99`) and
confirm the test run fails with exit code 14. A decimal such as `99.9` is rejected as an argument instead: the run
reports "Zero tests ran", which proves nothing about the gate.

## 5. Remote and CI

These need the user's accounts; give them the steps rather than doing them:

1. Create the remote repository; push `main`.
2. Protect `main`; require a passing pipeline to merge; squash merges.
3. Watch the first pipeline: every job green, including `godot-smoke` and `wiki`. If `godot-smoke` can't download
   Godot, check that `GODOT_VERSION` is a real `godotengine/godot-builds` release tag (e.g. `4.5.1-stable`).
4. Enable Pages (members-only for a private project) and open the published wiki.
5. Tick the Phase 0 boxes in `docs/IMPLEMENTATION_PLAN.md`.
