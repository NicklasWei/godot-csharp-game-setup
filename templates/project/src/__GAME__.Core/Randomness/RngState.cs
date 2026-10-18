namespace __GAME__.Core.Randomness;

/// <summary>
/// A snapshot of an <see cref="RngStream"/>'s position in its sequence, for saves: the seed it was created from plus
/// the four xoshiro256** state words.
/// </summary>
/// <param name="Seed">The seed the stream was created from; kept so derived streams stay reproducible.</param>
/// <param name="S0">The first state word.</param>
/// <param name="S1">The second state word.</param>
/// <param name="S2">The third state word.</param>
/// <param name="S3">The fourth state word.</param>
public readonly record struct RngState(ulong Seed, ulong S0, ulong S1, ulong S2, ulong S3);
