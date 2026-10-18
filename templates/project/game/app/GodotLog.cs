using System.Diagnostics.CodeAnalysis;
using __GAME__.Core.Diagnostics;
using Godot;

namespace __GAME__.Game.App;

/// <summary>
/// The game's <see cref="ILog"/> implementation. Writes to Godot's output: debug and info messages go to standard
/// output, warnings and errors to the editor's debugger panel (and to the headless log, where CI checks for them).
/// </summary>
/// <remarks>
/// This is the only class allowed to call <c>GD.Print</c> and friends. Add a rotating file log and an in-game console
/// sink here when they are needed.
/// </remarks>
[SuppressMessage(
    "ApiDesign",
    "RS0030:Do not use banned APIs",
    Justification = "GodotLog is the single sanctioned wrapper around Godot's print functions."
)]
public sealed class GodotLog : ILog
{
    /// <summary>The lowest level that is written; anything less severe is discarded.</summary>
    private readonly LogLevel _minimumLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="GodotLog"/> class.
    /// </summary>
    /// <param name="minimumLevel">The lowest level to write.</param>
    public GodotLog(LogLevel minimumLevel)
    {
        _minimumLevel = minimumLevel;
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel level) => level >= _minimumLevel;

    /// <inheritdoc/>
    public void Write(LogLevel level, string category, string message)
    {
        if (!IsEnabled(level))
        {
            return;
        }

        string line = $"[{level}] [{category}] {message}";
        switch (level)
        {
            case LogLevel.Error:
                GD.PushError(line);
                break;
            case LogLevel.Warning:
                GD.PushWarning(line);
                break;
            case LogLevel.Debug:
            case LogLevel.Info:
            default:
                GD.Print(line);
                break;
        }
    }
}
