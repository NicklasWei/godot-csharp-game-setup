using __GAME__.Core.Content;
using __GAME__.Core.Development;
using __GAME__.Core.Diagnostics;
using __GAME__.Game.App;
using __GAME__.Game.L10n;

namespace __GAME__.RuntimeTests.Support;

/// <summary>
/// Builds the <see cref="AppServices"/> a screen needs in a runtime test, without booting <c>Main</c>.
/// </summary>
public static class TestServices
{
    /// <summary>The build version tests pass in, so they can find it on screen.</summary>
    public const string BuildVersion = "test-build";

    /// <summary>
    /// Creates services with real Godot-backed implementations, no content, and no dev flags.
    /// </summary>
    /// <returns>The services.</returns>
    public static AppServices Create()
    {
        var log = new GodotLog(LogLevel.Warning);
        return new AppServices(log, new GodotClock(), new LocaleService(log), DevFlags.None, BuildVersion, ContentDatabase.Empty);
    }
}
