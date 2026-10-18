namespace __GAME__.Core.Diagnostics;

/// <summary>
/// An <see cref="ILog"/> that discards everything, for code paths and tests that need a log but don't check it.
/// </summary>
public sealed class NullLog : ILog
{
    /// <summary>Gets the shared instance. It holds no state, so sharing it is safe.</summary>
    public static NullLog Instance { get; } = new();

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel level) => false;

    /// <inheritdoc/>
    public void Write(LogLevel level, string category, string message) { }
}
