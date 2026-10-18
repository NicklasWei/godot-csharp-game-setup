namespace __GAME__.Core.Randomness;

/// <summary>
/// A seeded, deterministic pseudo-random stream: the only source of gameplay randomness (Coding Standards §3.6,
/// ADR 0009). Give each independent consumer (world generation, loot, combat) its own stream, so one system's draws
/// never shift another's.
/// </summary>
/// <remarks>
/// <para>
/// The generator is xoshiro256** (Blackman and Vigna), with its 256-bit state filled from the seed by
/// <see cref="SplitMix64"/> as the authors recommend. The output sequence is part of the save and seed-sharing
/// contract: changing the algorithm or any method's use of it changes every seeded result. Golden tests pin it.
/// </para>
/// <para>
/// Every draw is allocation-free. Bounded integers use Lemire's multiply-and-reject method, so they
/// are unbiased for every range. Not thread-safe; each stream belongs to one simulation.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "A random-number stream, not a System.IO.Stream; the name is fixed by the coding standards."
)]
public sealed class RngStream
{
    /// <summary>The scale from a 24-bit integer to a float in [0, 1).</summary>
    private const float FloatUnit = 1f / (1 << 24);

    /// <summary>The scale from a 53-bit integer to a double in [0, 1).</summary>
    private const double DoubleUnit = 1.0 / (1UL << 53);

    /// <summary>The first state word.</summary>
    private ulong _s0;

    /// <summary>The second state word.</summary>
    private ulong _s1;

    /// <summary>The third state word.</summary>
    private ulong _s2;

    /// <summary>The fourth state word.</summary>
    private ulong _s3;

    /// <summary>
    /// Initializes a new instance of the <see cref="RngStream"/> class from a seed.
    /// </summary>
    /// <param name="seed">Any 64-bit value, including zero.</param>
    public RngStream(ulong seed)
    {
        Seed = seed;
        var expander = new SplitMix64(seed);
        _s0 = expander.Next();
        _s1 = expander.Next();
        _s2 = expander.Next();
        _s3 = expander.Next();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RngStream"/> class that resumes a snapshot.
    /// </summary>
    /// <param name="state">A snapshot from <see cref="State"/>.</param>
    /// <exception cref="ArgumentException">All four state words are zero (xoshiro would only output zeros).</exception>
    public RngStream(RngState state)
    {
        if ((state.S0 | state.S1 | state.S2 | state.S3) == 0)
        {
            throw new ArgumentException("An all-zero xoshiro256** state is invalid.", nameof(state));
        }

        Seed = state.Seed;
        _s0 = state.S0;
        _s1 = state.S1;
        _s2 = state.S2;
        _s3 = state.S3;
    }

    /// <summary>Gets the seed this stream was created from (unchanged by draws).</summary>
    public ulong Seed { get; }

    /// <summary>Gets a snapshot of the current position in the sequence.</summary>
    public RngState State => new(Seed, _s0, _s1, _s2, _s3);

    /// <summary>
    /// Creates an independent child stream keyed by <paramref name="key"/>, without advancing this one.
    /// </summary>
    /// <remarks>
    /// The child depends only on <see cref="Seed"/> and the key, so it is the same however many draws this
    /// stream has made. Use it to give each level, room, or encounter its own sequence. Allocates; load-time only.
    /// </remarks>
    /// <param name="key">Any stable identifier, such as a level index.</param>
    /// <returns>The child stream.</returns>
    public RngStream Derive(ulong key) => new(SplitMix64.Mix(Seed ^ SplitMix64.Mix(key)));

    /// <summary>
    /// Advances the stream and returns 64 random bits.
    /// </summary>
    /// <returns>A value uniform over the full <see cref="ulong"/> range.</returns>
    public ulong NextUInt64()
    {
        unchecked
        {
            ulong result = ulong.RotateLeft(_s1 * 5, 7) * 9;
            ulong t = _s1 << 17;
            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = ulong.RotateLeft(_s3, 45);
            return result;
        }
    }

    /// <summary>
    /// Returns 32 random bits (the high half of one 64-bit draw).
    /// </summary>
    /// <returns>A value uniform over the full <see cref="uint"/> range.</returns>
    public uint NextUInt32() => (uint)(NextUInt64() >> 32);

    /// <summary>
    /// Returns a uniform integer in [0, <paramref name="maxExclusive"/>).
    /// </summary>
    /// <param name="maxExclusive">The exclusive upper bound; must be positive.</param>
    /// <returns>The integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxExclusive"/> is zero or negative.</exception>
    public int NextInt(int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
        return (int)NextBelow((uint)maxExclusive);
    }

    /// <summary>
    /// Returns a uniform integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).
    /// </summary>
    /// <param name="minInclusive">The inclusive lower bound.</param>
    /// <param name="maxExclusive">The exclusive upper bound; must be greater than <paramref name="minInclusive"/>.</param>
    /// <returns>The integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The range is empty.</exception>
    public int NextInt(int minInclusive, int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxExclusive, minInclusive);
        uint range = (uint)((long)maxExclusive - minInclusive);
        return (int)(minInclusive + NextBelow(range));
    }

    /// <summary>
    /// Returns a uniform float in [0, 1) with 24 bits of precision (every representable step equally likely).
    /// </summary>
    /// <returns>The float.</returns>
    public float NextFloat() => (NextUInt64() >> 40) * FloatUnit;

    /// <summary>
    /// Returns a uniform float in [<paramref name="min"/>, <paramref name="max"/>).
    /// </summary>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The exclusive upper bound; must not be less than <paramref name="min"/>.</param>
    /// <returns>The float; <paramref name="min"/> when the bounds are equal.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="max"/> is less than <paramref name="min"/>.</exception>
    public float NextFloat(float min, float max)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(max, min);
        float value = min + ((max - min) * NextFloat());

        // Rounding can land exactly on max for some bounds; keep the upper bound exclusive.
        return value < max ? value : MathF.Max(min, MathF.BitDecrement(max));
    }

    /// <summary>
    /// Returns a uniform double in [0, 1) with 53 bits of precision.
    /// </summary>
    /// <returns>The double.</returns>
    public double NextDouble() => (NextUInt64() >> 11) * DoubleUnit;

    /// <summary>
    /// Returns <see langword="true"/> or <see langword="false"/> with equal probability.
    /// </summary>
    /// <returns>The coin flip.</returns>
    public bool NextBool() => (NextUInt64() >> 63) != 0;

    /// <summary>
    /// Rolls an event that happens with the given probability. Always draws once, so the stream advances
    /// the same way whatever the probability (keeps sequences aligned when a chance changes).
    /// </summary>
    /// <param name="probability">The chance from 0 to 1; values outside clamp (≤ 0 never, ≥ 1 always), NaN never.</param>
    /// <returns><see langword="true"/> when the event happens.</returns>
    public bool Chance(float probability) => NextFloat() < probability;

    /// <summary>
    /// Shuffles a span in place (Fisher–Yates); every permutation is equally likely.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">The items to shuffle.</param>
    public void Shuffle<T>(Span<T> items)
    {
        for (int i = items.Length - 1; i > 0; i--)
        {
            int j = NextInt(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    /// <summary>
    /// Picks an index with probability proportional to its weight, as for loot or spawn tables.
    /// </summary>
    /// <param name="weights">Non-negative integer weights with a positive total that fits in an <see cref="int"/>.</param>
    /// <returns>The picked index; zero-weight indices are never picked.</returns>
    /// <exception cref="ArgumentException">A weight is negative, or the total is zero.</exception>
    /// <exception cref="OverflowException">The total overflows an <see cref="int"/>.</exception>
    public int PickWeighted(ReadOnlySpan<int> weights)
    {
        int total = 0;
        foreach (int weight in weights)
        {
            if (weight < 0)
            {
                throw new ArgumentException("Weights must not be negative.", nameof(weights));
            }

            total = checked(total + weight);
        }

        if (total == 0)
        {
            throw new ArgumentException("At least one weight must be positive.", nameof(weights));
        }

        int roll = NextInt(total);
        for (int i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll < 0)
            {
                return i;
            }
        }

        // Unreachable: roll < total, and the weights sum to total.
        throw new InvalidOperationException("Weighted pick ran past the last weight.");
    }

    /// <summary>
    /// Returns a uniform integer in [0, <paramref name="bound"/>) without modulo bias (Lemire 2019).
    /// </summary>
    /// <param name="bound">The exclusive upper bound; must be positive.</param>
    /// <returns>The integer.</returns>
    private uint NextBelow(uint bound)
    {
        ulong product = (ulong)NextUInt32() * bound;
        uint low = (uint)product;
        if (low < bound)
        {
            // Reject the few low products that would over-represent some results.
            uint threshold = unchecked(0u - bound) % bound;
            while (low < threshold)
            {
                product = (ulong)NextUInt32() * bound;
                low = (uint)product;
            }
        }

        return (uint)(product >> 32);
    }
}
