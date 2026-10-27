# Architecture Decision Records

Each decision gets one file, `NNNN-kebab-title.md`, with **Context / Decision / Consequences**, under one page.
Accepted ADRs are immutable. To change a decision, write a new ADR that supersedes the old one, and edit only the
old one's status line. Spikes end in an ADR that records the measurements behind the decision.

An ADR is history: it may give the values its decision was made around, as of its date, while the code holds the
current ones. Describe any other value instead of giving it precisely
([Coding Standards §6.2](../CODING_STANDARDS.md#62-docs-and-values)).

ADRs 0001–0011 came with the project template; they record the foundation's decisions and why they were made.

| ADR | Title | Status |
|---|---|---|
| [0001](0001-core-game-split.md) | Engine-free Core library and a thin Godot layer | Accepted |
| [0002](0002-sdk-pinning.md) | The .NET SDK and the Godot SDK are pinned exactly, locally and in CI | Accepted |
| [0003](0003-testing-stack.md) | Testing stack: xUnit v3 on Microsoft.Testing.Platform, with project files tested as data | Accepted |
| [0004](0004-formatting-and-analyzers.md) | CSharpier formatting and a strict analyzer set | Accepted |
| [0005](0005-localization.md) | Key-based gettext PO localization | Accepted |
| [0006](0006-godot-runtime-tests-and-ci.md) | Godot-runtime tests run in an in-repo headless runner, in CI | Accepted |
| [0007](0007-data-driven-content.md) | Content is JSON in registered kinds, with permanent IDs and generated schemas | Accepted |
| [0008](0008-developer-wiki.md) | A developer wiki generated from the code, built with DocFX | Accepted |
| [0009](0009-deterministic-randomness.md) | Gameplay randomness is seeded xoshiro256\*\* streams | Accepted |
| [0010](0010-justfile-task-runner.md) | Project commands run through a justfile | Accepted |
| [0011](0011-export-presets.md) | Export presets for Windows and Linux are committed, and just exports them | Accepted |

## Template

```markdown
# NNNN: A sentence that states the decision

**Status:** Proposed | Accepted (YYYY-MM-DD) | Superseded by [NNNN](NNNN-….md)

## Context

What forces the decision: the problem, constraints, and options considered (with measurements, for a spike).

## Decision

What was decided, specifically enough to check code against.

## Consequences

What follows, good and bad: new rules, costs, follow-up work, what would make us revisit it.
```
