namespace __GAME__.Core.Diagnostics;

/// <summary>
/// Writes categorized log messages. Core code logs only through this interface; the Godot layer
/// supplies the implementation.
/// </summary>
/// <remarks>
/// Implementations must be safe to call from the main thread at any time. In per-frame code,
/// check <see cref="IsEnabled"/> before building a message so disabled levels cost nothing.
/// </remarks>
public interface ILog
{
    /// <summary>
    /// Returns whether messages at <paramref name="level"/> are currently written.
    /// </summary>
    /// <param name="level">The level to check.</param>
    /// <returns><see langword="true"/> if a message at that level would be written.</returns>
    bool IsEnabled(LogLevel level);

    /// <summary>
    /// Writes a message.
    /// </summary>
    /// <param name="level">The message severity.</param>
    /// <param name="category">A short subsystem name such as <c>"Save"</c> or <c>"Content"</c>.</param>
    /// <param name="message">The message text. Not localized; logs are developer-facing.</param>
    void Write(LogLevel level, string category, string message);
}
