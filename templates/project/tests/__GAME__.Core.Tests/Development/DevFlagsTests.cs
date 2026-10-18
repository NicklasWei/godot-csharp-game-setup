using __GAME__.Core.Development;

namespace __GAME__.Core.Tests.Development;

/// <summary>Tests for <see cref="DevFlags"/>.</summary>
public sealed class DevFlagsTests
{
    [Fact]
    public void Parse_NoArguments_IsNormalPlay()
    {
        // Act
        DevFlags flags = DevFlags.Parse([], out IReadOnlyList<string> unknown);

        // Assert
        Assert.Equal(DevFlags.None, flags);
        Assert.Empty(unknown);
    }

    [Fact]
    public void Parse_KnownFlags_SetsThem()
    {
        // Act
        DevFlags flags = DevFlags.Parse(
            [DevFlags.AutopilotArgument, DevFlags.PseudolocalizeArgument],
            out IReadOnlyList<string> unknown
        );

        // Assert
        Assert.True(flags.Autopilot);
        Assert.True(flags.Pseudolocalize);
        Assert.Empty(unknown);
    }

    [Fact]
    public void Parse_UnknownArgument_IsReportedNotFatal()
    {
        // Act
        DevFlags flags = DevFlags.Parse(["--typo", DevFlags.AutopilotArgument], out IReadOnlyList<string> unknown);

        // Assert
        Assert.True(flags.Autopilot);
        Assert.Equal(["--typo"], unknown);
    }
}
