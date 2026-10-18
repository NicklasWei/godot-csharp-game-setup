using System.Diagnostics.CodeAnalysis;
using __GAME__.Core.Diagnostics;
using Godot;

namespace __GAME__.Game.App;

/// <summary>
/// The game's <see cref="IClock"/> implementation, backed by Godot's monotonic tick counter and the system wall clock.
/// </summary>
public sealed class GodotClock : IClock
{
    /// <inheritdoc/>
    [SuppressMessage(
        "ApiDesign",
        "RS0030:Do not use banned APIs",
        Justification = "GodotClock is the single sanctioned source of wall-clock time."
    )]
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    public TimeSpan Elapsed => TimeSpan.FromMicroseconds(Time.GetTicksUsec());
}
