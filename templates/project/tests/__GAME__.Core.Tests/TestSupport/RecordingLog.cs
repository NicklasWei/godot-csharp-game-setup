using __GAME__.Core.Diagnostics;

namespace __GAME__.Core.Tests.TestSupport;

/// <summary>
/// An <see cref="ILog"/> fake that records every message, for tests that assert what was logged.
/// </summary>
internal sealed class RecordingLog : ILog
{
    private readonly List<(LogLevel Level, string Category, string Message)> _entries = [];

    /// <summary>Gets every message written so far, oldest first.</summary>
    public IReadOnlyList<(LogLevel Level, string Category, string Message)> Entries => _entries;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel level) => true;

    /// <inheritdoc/>
    public void Write(LogLevel level, string category, string message) => _entries.Add((level, category, message));
}
