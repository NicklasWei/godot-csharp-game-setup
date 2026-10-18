# 0004: CSharpier formatting and a strict analyzer set

**Status:** Accepted (__DATE__)

## Context

The standards require strict, tool-enforced formatting, documentation, and API bans, with no debate over layout.

## Decision

- **Formatting:** CSharpier, a local dotnet tool (`.config/dotnet-tools.json`, `rollForward: false`), with
  `printWidth: 120`. It also formats MSBuild XML and sorts `using` directives. `dotnet csharpier check .` gates CI
  and the pre-commit hook.
- **Analyzers** in `Directory.Build.props`, warnings as errors: .NET analyzers at `latest-recommended`,
  StyleCop.Analyzers, Roslynator.Analyzers, BannedApiAnalyzers.
- **Docs:** CS1591 and SA1600 with `documentPrivateElements: true`, so private methods need docs; private fields are
  exempt; test methods are exempt.
- **StyleCop vs CSharpier:** the layout and spacing rules that conflict with CSharpier's output are off (SA1000,
  SA1008–SA1011, SA1111, SA1116–SA1118, SA1127–SA1128, SA1137, SA1413, SA1500–SA1504, SA1515–SA1516), and IDE0055.
- **Banned APIs:** `BannedSymbols.Core.txt` and `BannedSymbols.Game.txt`. In the game layer `System.Console` is banned
  per method, not as a type, because Godot's generated code references `Console` and generated sources can't be
  excluded by an `.editorconfig` path. Each overload needs its own documentation ID.

## Consequences

- A missing doc (public or private), a banned API, and misformatted code each fail the gate; Phase 0 verifies this.
- StyleCop is in a long-running beta: keep the version pinned and revisit if it breaks on new C# syntax.
- `using` order depends on the root namespace's name relative to `Godot` and `System`; CSharpier settles it.
