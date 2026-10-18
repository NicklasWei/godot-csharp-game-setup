using __GAME__.Core.Randomness;

namespace __GAME__.Core.Tests.Randomness;

/// <summary>Tests for <see cref="SplitMix64"/>, pinned to the reference implementation's outputs.</summary>
public sealed class SplitMix64Tests
{
    [Fact]
    public void Next_Seed1234567_MatchesReferenceSequence()
    {
        // Arrange: reference values from Vigna's splitmix64.c, seeded with 1234567.
        ulong[] expected =
        [
            6457827717110365317UL,
            3203168211198807973UL,
            9817491932198370423UL,
            4593380528125082431UL,
            16408922859458223821UL,
        ];
        var generator = new SplitMix64(1234567UL);

        // Act
        ulong[] actual = [.. Enumerable.Range(0, expected.Length).Select(_ => generator.Next())];

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Next_SeedZero_ProducesNonZeroReferenceOutput()
    {
        // Arrange
        var generator = new SplitMix64(0UL);

        // Act
        ulong first = generator.Next();

        // Assert
        Assert.Equal(16294208416658607535UL, first);
    }

    [Fact]
    public void Next_CopiedGenerator_ProducesIdenticalSequence()
    {
        // Arrange
        var original = new SplitMix64(42UL);
        original.Next();
        SplitMix64 copy = original;

        // Act
        ulong fromOriginal = original.Next();
        ulong fromCopy = copy.Next();

        // Assert
        Assert.Equal(fromOriginal, fromCopy);
    }

    [Fact]
    public void Mix_DistinctInputs_ProduceDistinctOutputs()
    {
        // Act
        ulong[] mixed = [.. Enumerable.Range(0, 1000).Select(id => SplitMix64.Mix((ulong)id))];

        // Assert
        Assert.Equal(mixed.Length, mixed.Distinct().Count());
    }
}
