namespace __GAME__.Core.Randomness;

/// <summary>
/// The SplitMix64 pseudo-random generator: a tiny, fast 64-bit generator used to expand one seed into
/// well-distributed seeds and state words (ADR 0009).
/// </summary>
/// <remarks>
/// <para>
/// This is a mutable value type. Copying it copies the generator state, so two copies produce the
/// same sequence. Pass it by <see langword="ref"/> when a caller should advance the original.
/// </para>
/// <para>
/// The output sequence is part of the save and seed-sharing contract: changing the algorithm
/// changes every generated result. The implementation follows Sebastiano Vigna's reference version.
/// It is allocation-free.
/// </para>
/// </remarks>
public struct SplitMix64
{
    /// <summary>The golden-ratio increment added to the state on every step.</summary>
    private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;

    /// <summary>The current internal state; advanced by <see cref="GoldenGamma"/> on each call.</summary>
    private ulong _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="SplitMix64"/> struct.
    /// </summary>
    /// <param name="seed">The initial state. Every value, including zero, is a valid seed.</param>
    public SplitMix64(ulong seed)
    {
        _state = seed;
    }

    /// <summary>
    /// Applies the SplitMix64 finalizer to a value without touching any generator state.
    /// </summary>
    /// <remarks>
    /// Useful for deriving a stream seed deterministically from a master seed and a stream identifier,
    /// e.g. <c>Mix(seed ^ Mix(streamId))</c>.
    /// </remarks>
    /// <param name="value">The value to scramble.</param>
    /// <returns>The scrambled value. Distinct inputs produce distinct outputs (the function is a bijection).</returns>
    public static ulong Mix(ulong value)
    {
        unchecked
        {
            ulong z = value;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    /// <summary>
    /// Advances the generator and returns the next 64-bit output.
    /// </summary>
    /// <returns>The next pseudo-random value, uniformly distributed over the full <see cref="ulong"/> range.</returns>
    public ulong Next()
    {
        _state = unchecked(_state + GoldenGamma);
        return Mix(_state);
    }
}
