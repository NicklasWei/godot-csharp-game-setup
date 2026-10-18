# 0003: Testing stack: xUnit v3 on Microsoft.Testing.Platform, with project files tested as data

**Status:** Accepted (__DATE__)

## Context

Core needs a standard unit-test framework with a coverage gate. Some checks concern Godot project files (the input
map, translated scene text, registered catalogs) and don't need an engine; others do (scenes loading, screens in the
live tree), see ADR 0006.

## Decision

- **Core tests:** xUnit v3 on **Microsoft.Testing.Platform**, opted in through `global.json`
  (`"test": { "runner": "Microsoft.Testing.Platform" }`). The VSTest packages (`Microsoft.NET.Test.Sdk`,
  `xunit.runner.visualstudio`, `coverlet.msbuild`) aren't used. Run with `dotnet test --project tests/__GAME__.Core.Tests`.
- **Coverage:** `coverlet.MTP`, Cobertura output, `--coverlet-threshold 85 --coverlet-threshold-type line` on
  `[__GAME__.Core]*`. Missing the threshold fails the run.
- **Project files as data:** the input map, scene text, PO catalogs, real content, and generated schemas are checked
  by parsing the files in the Core test project. They run anywhere, in milliseconds.
- **No FluentAssertions** (commercial license from v8); fakes, not a mocking library.
- **Allocation tests** take the lowest of up to three measurements: coverage instrumentation and JIT tiering
  occasionally allocate once on a first-time path, which made single measurements flaky in the coverage job.

## Consequences

- IDE test explorers must support MTP; current Visual Studio and Rider do.
- Adding a project-wide rule (a naming convention, a required file) usually means adding a file-based test, not a
  checklist item.
