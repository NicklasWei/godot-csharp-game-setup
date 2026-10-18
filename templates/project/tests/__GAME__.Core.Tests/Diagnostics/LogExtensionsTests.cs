using __GAME__.Core.Diagnostics;
using __GAME__.Core.Tests.TestSupport;

namespace __GAME__.Core.Tests.Diagnostics;

/// <summary>Tests for <see cref="LogExtensions"/> and <see cref="NullLog"/>.</summary>
public sealed class LogExtensionsTests
{
    [Fact]
    public void Shorthands_WriteAtTheirLevels()
    {
        // Arrange
        var log = new RecordingLog();

        // Act
        log.Debug("Test", "d");
        log.Info("Test", "i");
        log.Warn("Test", "w");
        log.Error("Test", "e");

        // Assert
        Assert.Equal(
            [LogLevel.Debug, LogLevel.Info, LogLevel.Warning, LogLevel.Error],
            log.Entries.Select(entry => entry.Level)
        );
    }

    [Fact]
    public void NullLog_IsNeverEnabled()
    {
        // Act
        NullLog.Instance.Info("Test", "discarded");

        // Assert
        Assert.False(NullLog.Instance.IsEnabled(LogLevel.Error));
    }
}
