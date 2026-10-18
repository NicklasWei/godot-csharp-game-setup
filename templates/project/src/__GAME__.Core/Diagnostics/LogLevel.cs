namespace __GAME__.Core.Diagnostics;

/// <summary>
/// The severity of a log message, ordered from most to least verbose.
/// </summary>
public enum LogLevel
{
    /// <summary>Detailed diagnostic information, normally only enabled in development builds.</summary>
    Debug = 0,

    /// <summary>Notable but expected events, such as scene changes or saves.</summary>
    Info = 1,

    /// <summary>Something unexpected that the game recovered from.</summary>
    Warning = 2,

    /// <summary>A failure that broke an operation and needs attention.</summary>
    Error = 3,
}
