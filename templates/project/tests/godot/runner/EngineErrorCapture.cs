using Godot;

namespace __GAME__.RuntimeTests.Runner;

/// <summary>
/// A Godot <see cref="Logger"/> that records engine, script, and shader errors, so the runner can fail any test that
/// triggered one, even when the error never surfaced as an exception (a missing resource, a shader compile error).
/// </summary>
/// <remarks>Godot may log from any thread, so access is locked. Warnings are not recorded. Needs Godot 4.5+.</remarks>
public sealed partial class EngineErrorCapture : Logger
{
    /// <summary>Godot's error type for warnings (<c>Logger.ErrorType.WARNING</c>).</summary>
    private const int WarningType = 1;

    /// <summary>Guards <see cref="_errors"/>.</summary>
    private readonly Lock _gate = new();

    /// <summary>The errors since the last <see cref="Take"/>.</summary>
    private readonly List<string> _errors = [];

    /// <summary>
    /// Returns the recorded errors and clears them.
    /// </summary>
    /// <returns>The errors, oldest first.</returns>
    public string[] Take()
    {
        lock (_gate)
        {
            string[] errors = [.. _errors];
            _errors.Clear();
            return errors;
        }
    }

    /// <inheritdoc/>
    public override void _LogError(
        string function,
        string file,
        int line,
        string code,
        string rationale,
        bool editorNotify,
        int errorType,
        Godot.Collections.Array<ScriptBacktrace> scriptBacktraces
    )
    {
        if (errorType == WarningType)
        {
            return;
        }

        string text = string.IsNullOrEmpty(rationale) ? code : rationale;
        lock (_gate)
        {
            _errors.Add($"{text} (at {file}:{line} in {function})");
        }
    }
}
