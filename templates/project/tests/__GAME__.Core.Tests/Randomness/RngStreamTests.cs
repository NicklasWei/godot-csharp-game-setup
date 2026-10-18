using __GAME__.Core.Randomness;
using __GAME__.Core.Tests.TestSupport;

namespace __GAME__.Core.Tests.Randomness;

/// <summary>
/// Tests for <see cref="RngStream"/>: reference outputs (the golden values that pin every seed), determinism,
/// bounds, and allocation-free draws.
/// </summary>
public sealed class RngStreamTests
{
    [Fact]
    public void NextUInt64_State1234_MatchesReferenceSequence()
    {
        // Arrange: reference values from Vigna's xoshiro256starstar.c with the state {1, 2, 3, 4}.
        var stream = new RngStream(new RngState(0UL, 1UL, 2UL, 3UL, 4UL));
        ulong[] expected = [0x2D00UL, 0UL, 0x5A007080UL, 0x10E0000000009D80UL, 0x10E0B61CE1009D80UL];

        // Act
        ulong[] actual = [.. Enumerable.Range(0, expected.Length).Select(_ => stream.NextUInt64())];

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NextUInt64_Seeded_MatchesSplitMixExpandedReference()
    {
        // Arrange: computed by an independent Python port of both reference generators.
        var stream = new RngStream(20261009UL);

        // Act & Assert
        Assert.Equal(0x43BB3D5E609D1E58UL, stream.NextUInt64());
        Assert.Equal(0xA479139F0B6C664EUL, stream.NextUInt64());
        Assert.Equal(0x3FEB6A4B877FF51DUL, stream.NextUInt64());
    }

    [Fact]
    public void Constructor_SameSeed_ProducesSameSequence()
    {
        // Arrange
        var first = new RngStream(42UL);
        var second = new RngStream(42UL);

        // Act & Assert
        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(first.NextUInt64(), second.NextUInt64());
        }
    }

    [Fact]
    public void State_RestoredSnapshot_ResumesTheSameSequence()
    {
        // Arrange
        var original = new RngStream(7UL);
        for (int i = 0; i < 17; i++)
        {
            original.NextUInt64();
        }

        // Act
        var resumed = new RngStream(original.State);

        // Assert
        Assert.Equal(original.Seed, resumed.Seed);
        Assert.Equal(original.NextUInt64(), resumed.NextUInt64());
    }

    [Fact]
    public void Constructor_AllZeroState_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RngStream(new RngState(1UL, 0UL, 0UL, 0UL, 0UL)));
    }

    [Fact]
    public void Derive_DependsOnlyOnSeedAndKey()
    {
        // Arrange
        var fresh = new RngStream(99UL);
        var used = new RngStream(99UL);
        used.NextUInt64();

        // Act
        ulong fromFresh = fresh.Derive(3UL).NextUInt64();
        ulong fromUsed = used.Derive(3UL).NextUInt64();
        ulong otherKey = fresh.Derive(4UL).NextUInt64();

        // Assert
        Assert.Equal(fromFresh, fromUsed);
        Assert.NotEqual(fromFresh, otherKey);
    }

    [Fact]
    public void NextInt_StaysInRangeAndCoversIt()
    {
        // Arrange
        var stream = new RngStream(1UL);
        var seen = new HashSet<int>();

        // Act
        for (int i = 0; i < 10_000; i++)
        {
            int value = stream.NextInt(7);
            Assert.InRange(value, 0, 6);
            seen.Add(value);
        }

        // Assert
        Assert.Equal(7, seen.Count);
    }

    [Fact]
    public void NextInt_MinMax_StaysInRangeIncludingNegatives()
    {
        // Arrange
        var stream = new RngStream(2UL);

        // Act & Assert
        for (int i = 0; i < 10_000; i++)
        {
            Assert.InRange(stream.NextInt(-5, 5), -5, 4);
        }

        Assert.InRange(stream.NextInt(int.MinValue, int.MaxValue), int.MinValue, int.MaxValue - 1);
    }

    [Fact]
    public void NextInt_EmptyRange_Throws()
    {
        // Arrange
        var stream = new RngStream(3UL);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.NextInt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.NextInt(5, 5));
    }

    [Fact]
    public void NextFloatAndDouble_StayInUnitRange()
    {
        // Arrange
        var stream = new RngStream(4UL);

        // Act & Assert
        for (int i = 0; i < 10_000; i++)
        {
            float single = stream.NextFloat();
            double precise = stream.NextDouble();
            Assert.True(single is >= 0f and < 1f);
            Assert.True(precise is >= 0.0 and < 1.0);
        }
    }

    [Fact]
    public void NextFloat_Bounds_AreRespected()
    {
        // Arrange
        var stream = new RngStream(5UL);

        // Act & Assert
        for (int i = 0; i < 10_000; i++)
        {
            float value = stream.NextFloat(-2f, 3f);
            Assert.True(value is >= -2f and < 3f);
        }

        Assert.Equal(1.5f, stream.NextFloat(1.5f, 1.5f));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.NextFloat(2f, 1f));
    }

    [Fact]
    public void NextBool_ProducesBothValues()
    {
        // Arrange
        var stream = new RngStream(6UL);

        // Act
        bool[] flips = [.. Enumerable.Range(0, 100).Select(_ => stream.NextBool())];

        // Assert
        Assert.Contains(true, flips);
        Assert.Contains(false, flips);
    }

    [Fact]
    public void Chance_ZeroAndOne_AreNeverAndAlways()
    {
        // Arrange
        var stream = new RngStream(7UL);

        // Act & Assert
        for (int i = 0; i < 1000; i++)
        {
            Assert.False(stream.Chance(0f));
            Assert.True(stream.Chance(1f));
        }
    }

    [Fact]
    public void Shuffle_KeepsEveryItem()
    {
        // Arrange
        var stream = new RngStream(8UL);
        int[] items = [.. Enumerable.Range(0, 50)];

        // Act
        stream.Shuffle<int>(items);

        // Assert
        Assert.Equal(Enumerable.Range(0, 50), items.Order());
        Assert.NotEqual(Enumerable.Range(0, 50), items);
    }

    [Fact]
    public void PickWeighted_ZeroWeights_AreNeverPicked()
    {
        // Arrange
        var stream = new RngStream(9UL);
        int[] weights = [0, 3, 0, 1];

        // Act
        int[] picks = [.. Enumerable.Range(0, 1000).Select(_ => stream.PickWeighted(weights))];

        // Assert
        Assert.DoesNotContain(0, picks);
        Assert.DoesNotContain(2, picks);
        Assert.Contains(1, picks);
        Assert.Contains(3, picks);
    }

    [Fact]
    public void PickWeighted_BadWeights_Throw()
    {
        // Arrange
        var stream = new RngStream(10UL);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => stream.PickWeighted([1, -1]));
        Assert.Throws<ArgumentException>(() => stream.PickWeighted([0, 0]));
    }

    [Fact]
    public void Draws_DoNotAllocate()
    {
        // Arrange
        var stream = new RngStream(11UL);
        int[] weights = [1, 2, 3];
        DrawAll(stream, weights);

        // Act
        long allocated = Allocations.Measure(() => DrawAll(stream, weights));

        // Assert
        Assert.Equal(0, allocated);
    }

    /// <summary>
    /// Draws once from every kind of draw.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="weights">Weights for a weighted pick.</param>
    private static void DrawAll(RngStream stream, int[] weights)
    {
        for (int i = 0; i < 100; i++)
        {
            stream.NextUInt64();
            stream.NextInt(10);
            stream.NextInt(-3, 3);
            stream.NextFloat(0f, 2f);
            stream.NextDouble();
            stream.Chance(0.5f);
            stream.PickWeighted(weights);
        }
    }
}
