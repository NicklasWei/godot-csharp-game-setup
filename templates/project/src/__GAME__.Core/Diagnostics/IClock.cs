namespace __GAME__.Core.Diagnostics;

/// <summary>
/// Supplies time to Core code. Injected so that logic and tests stay deterministic; Core must
/// never read the system clock directly (the banned-API list enforces it).
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current wall-clock time in UTC. Use only for timestamps (save files, history),
    /// never for gameplay timing.
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Gets the monotonic time elapsed since the application started. Never goes backwards, and is
    /// unaffected by system clock changes.
    /// </summary>
    TimeSpan Elapsed { get; }
}
