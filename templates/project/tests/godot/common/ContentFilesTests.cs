using __GAME__.Game.Common;
using __GAME__.RuntimeTests.Runner;
using __GAME__.RuntimeTests.Support;

namespace __GAME__.RuntimeTests.Common;

/// <summary>
/// The real content loads through Godot's file API (the path exported builds use) with no errors.
/// </summary>
public sealed class ContentFilesTests
{
    [GodotTest]
    public static void LoadDatabase_RealContent_LoadsWithoutErrors()
    {
        // Arrange
        var log = new RecordingLog();

        // Act
        ContentFiles.LoadDatabase(log);

        // Assert
        GodotAssert.True(log.Errors.Count == 0, "Content errors: " + string.Join(" | ", log.Errors));
    }
}
