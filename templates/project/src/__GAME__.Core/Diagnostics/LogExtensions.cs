namespace __GAME__.Core.Diagnostics;

/// <summary>
/// Shorthands for <see cref="ILog.Write"/> at each level.
/// </summary>
/// <remarks>
/// These don't check <see cref="ILog.IsEnabled"/> before the caller builds its message, so per-frame code checks
/// it first and only then formats.
/// </remarks>
public static class LogExtensions
{
    /// <summary>
    /// Writes a <see cref="LogLevel.Debug"/> message.
    /// </summary>
    /// <param name="log">The log.</param>
    /// <param name="category">A short subsystem name.</param>
    /// <param name="message">The message text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="log"/> is <see langword="null"/>.</exception>
    public static void Debug(this ILog log, string category, string message)
    {
        ArgumentNullException.ThrowIfNull(log);
        log.Write(LogLevel.Debug, category, message);
    }

    /// <summary>
    /// Writes a <see cref="LogLevel.Info"/> message.
    /// </summary>
    /// <param name="log">The log.</param>
    /// <param name="category">A short subsystem name.</param>
    /// <param name="message">The message text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="log"/> is <see langword="null"/>.</exception>
    public static void Info(this ILog log, string category, string message)
    {
        ArgumentNullException.ThrowIfNull(log);
        log.Write(LogLevel.Info, category, message);
    }

    /// <summary>
    /// Writes a <see cref="LogLevel.Warning"/> message.
    /// </summary>
    /// <param name="log">The log.</param>
    /// <param name="category">A short subsystem name.</param>
    /// <param name="message">The message text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="log"/> is <see langword="null"/>.</exception>
    public static void Warn(this ILog log, string category, string message)
    {
        ArgumentNullException.ThrowIfNull(log);
        log.Write(LogLevel.Warning, category, message);
    }

    /// <summary>
    /// Writes a <see cref="LogLevel.Error"/> message.
    /// </summary>
    /// <param name="log">The log.</param>
    /// <param name="category">A short subsystem name.</param>
    /// <param name="message">The message text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="log"/> is <see langword="null"/>.</exception>
    public static void Error(this ILog log, string category, string message)
    {
        ArgumentNullException.ThrowIfNull(log);
        log.Write(LogLevel.Error, category, message);
    }
}
