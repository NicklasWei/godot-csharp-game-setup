# 0002: The .NET SDK and the Godot SDK are pinned exactly, locally and in CI

**Status:** Accepted (__DATE__)

## Context

- `GodotSharp` ships for one target framework per Godot version (Godot 4.8 ships only `net10.0`), so the project's
  target framework follows the engine, not habit. Check the package before assuming one.
- Godot.NET.Sdk must match the editor version exactly; Godot dev builds can change APIs between snapshots.
- A floating CI image (`dotnet/sdk:10.0`) silently moves to newer SDK feature bands. Feature bands ship new analyzer
  rules, and with warnings as errors that broke CI on findings the local SDK didn't report (in an earlier project,
  six IDE0062 errors appeared only in CI).

## Decision

- All projects target `__TFM__`. `__GAME__.csproj` pins `Godot.NET.Sdk/__GODOT_SDK__` to match the editor
  (`__GODOT_RELEASE__`), and CI downloads that exact Godot release (`GODOT_VERSION`).
- `global.json` pins the exact SDK `__DOTNET_SDK__` with `rollForward: latestPatch`, so a newer SDK installed locally
  can't move the build to another feature band. CI uses the image or setup step for that same SDK.
- **To upgrade:** change the SDK in `global.json` and CI in one commit, fixing any new analyzer findings in that
  commit. To upgrade Godot, bump the editor, `Godot.NET.Sdk`, and `GODOT_VERSION` together, then run every gate.

## Consequences

- Local and CI builds see the same analyzers: no "green locally, red in CI" from SDK drift.
- New SDK fixes arrive only when someone bumps the pin; plan a bump about once a quarter.
- Every developer machine needs an SDK in the pinned band (`dotnet --version` in the repository root shows it).
