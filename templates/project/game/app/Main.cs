using __GAME__.Core.Content;
using __GAME__.Core.Development;
using __GAME__.Core.Diagnostics;
using __GAME__.Game.Common;
using __GAME__.Game.L10n;
using __GAME__.Game.Ui.Title;
using Godot;

namespace __GAME__.Game.App;

/// <summary>
/// The composition root: the main scene's root node. Builds the application services, owns their lifetimes, and
/// hands them to the first screen. No other node constructs services (Coding Standards §3.4).
/// </summary>
/// <remarks>
/// As the game grows, the first screen gives way to a flow controller that follows a Core state machine
/// (Coding Standards §3.9); it still gets its services from here, through <c>Initialize</c> before <c>AddChild</c>.
/// </remarks>
public sealed partial class Main : Node
{
    /// <summary>The log category used by the composition root.</summary>
    private const string LogCategory = "App";

    /// <summary>The project setting holding the build version string.</summary>
    private const string VersionSetting = "application/config/version";

    /// <summary>The title screen scene shown after boot.</summary>
    [Export]
    private PackedScene _titleScene = null!;

    /// <inheritdoc/>
    public override void _Ready()
    {
        NodeGuard.RequireExport(this, _titleScene);

        var log = new GodotLog(OS.IsDebugBuild() ? LogLevel.Debug : LogLevel.Info);
        var clock = new GodotClock();
        DevFlags flags = DevFlags.Parse(OS.GetCmdlineUserArgs(), out IReadOnlyList<string> unknown);
        foreach (string argument in unknown)
        {
            log.Warn(LogCategory, $"Ignoring the unknown argument {argument}.");
        }

        var locale = new LocaleService(log);
        locale.Apply(flags);
        ContentDatabase content = ContentFiles.LoadDatabase(log);
        string version = ProjectSettings.GetSetting(VersionSetting, "0.0.0").AsString();
        log.Info(LogCategory, $"__TITLE__ {version} starting at {clock.UtcNow:O}.");

        var services = new AppServices(log, clock, locale, flags, version, content);
        TitleScreen title = _titleScene.Instantiate<TitleScreen>();
        title.Initialize(services);
        AddChild(title);
    }
}
