using __GAME__.Core.Diagnostics;

namespace __GAME__.RuntimeTests.Support;

/// <summary>
/// An <see cref="ILog"/> fake that records every message, for runtime tests that assert what a service logged.
/// </summary>
public sealed class RecordingLog : ILog
{
    private readonly List<string> _errors = [];

    /// <summary>Gets every error message written so far, oldest first.</summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel level) => true;

    /// <inheritdoc/>
    public void Write(LogLevel level, string category, string message)
    {
        if (level == LogLevel.Error)
        {
            _errors.Add($"[{category}] {message}");
        }
    }
}
