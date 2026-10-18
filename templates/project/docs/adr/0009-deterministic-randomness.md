# 0009: Gameplay randomness is seeded xoshiro256\*\* streams

**Status:** Accepted (__DATE__)

## Context

Reproducible randomness makes generators testable, bugs replayable from a seed, and saves resumable. Whatever
generator is chosen becomes part of two contracts at once: saves store its state, and a seed must produce the same
result on every machine and every future build. `System.Random`'s algorithm isn't part of its contract, and
`GD.Rand*` is engine state.

## Decision

- **Generator: xoshiro256\*\*** in Core's `RngStream`, its 256-bit state filled from a 64-bit seed by `SplitMix64`.
  64 bits per draw; unbiased bounded integers (Lemire); floats from the top 24 bits, doubles from the top 53;
  `Chance` always draws exactly once, so changing a probability never shifts later draws.
- **Streams:** one per independent consumer (world generation, rewards, combat, AI), each seeded from the session
  seed and a fixed salt with `SplitMix64.Mix`; `Derive(key)` gives an independent child (per level, room, encounter)
  without advancing the parent. Player actions must never shift the world or reward streams.
- **Pinned by tests:** the reference xoshiro256\*\* and SplitMix64 vectors, plus golden outputs for fixed seeds.
- **Banned:** `System.Random` and `GD.Rand*` (BannedApiAnalyzers). Visual-only randomness uses a separate cosmetic
  stream seeded from OS entropy.

## Consequences

- **Never change** the algorithm, the seed expansion, the salts, or how draw methods use bits: that needs a new ADR
  and a save migration, and invalidates every shared seed.
- Saves store each stream's `RngState` (seed plus four state words).
- Draws are allocation-free; `Derive` allocates, so it's for load time.
