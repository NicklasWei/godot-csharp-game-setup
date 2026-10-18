using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Xml.Linq;
using __GAME__.Core.Diagnostics;
using __GAME__.Game.App;
using Godot;

namespace __GAME__.RuntimeTests.Runner;

/// <summary>
/// The in-repo Godot-runtime test runner (ADR 0006): the root of <c>tests/godot/test_runner.tscn</c>. It finds every
/// <see cref="GodotTestAttribute"/> method in the game assembly, runs each in its own container node in the live
/// tree, and quits with exit code 1 if any failed (or none were found).
/// </summary>
/// <remarks>
/// <para>
/// A test fails when it throws, when it times out, or when Godot logged an engine, script, or shader error while it
/// ran or in the frame after (captured by <see cref="EngineErrorCapture"/>).
/// </para>
/// <para>
/// Run it headless with <c>"$GODOT_BIN" --headless --path . res://tests/godot/test_runner.tscn</c>. User arguments
/// after <c>--</c>: <c>--filter=TEXT</c> runs only tests whose full name contains TEXT, and <c>--junit=PATH</c>
/// writes a JUnit XML report for CI.
/// </para>
/// </remarks>
public sealed partial class GodotTestRunner : Node
{
    /// <summary>The log category.</summary>
    private const string LogCategory = "Tests";

    /// <summary>The filter argument prefix.</summary>
    private const string FilterPrefix = "--filter=";

    /// <summary>The JUnit report argument prefix.</summary>
    private const string JUnitPrefix = "--junit=";

    /// <summary>Where results are printed.</summary>
    private readonly GodotLog _log = new(LogLevel.Info);

    /// <inheritdoc/>
    public override void _Ready() => _ = RunAllAsync();

    /// <summary>
    /// Finds the runtime tests: public methods marked <see cref="GodotTestAttribute"/>, ordered by full name.
    /// </summary>
    /// <param name="filter">A substring of the full test name, or empty for all.</param>
    /// <returns>The tests.</returns>
    private static List<MethodInfo> Discover(string filter)
    {
        var tests = new List<MethodInfo>();
        foreach (Type type in typeof(GodotTestRunner).Assembly.GetTypes())
        {
            foreach (
                MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            )
            {
                if (
                    method.GetCustomAttribute<GodotTestAttribute>() is not null
                    && FullName(method).Contains(filter, StringComparison.Ordinal)
                )
                {
                    tests.Add(method);
                }
            }
        }

        tests.Sort((a, b) => string.CompareOrdinal(FullName(a), FullName(b)));
        return tests;
    }

    /// <summary>
    /// Gets a test's display name.
    /// </summary>
    /// <param name="method">The test method.</param>
    /// <returns><c>Class.Method</c>.</returns>
    private static string FullName(MethodInfo method) => $"{method.DeclaringType?.Name}.{method.Name}";

    /// <summary>
    /// Reads a <c>prefix=value</c> user argument.
    /// </summary>
    /// <param name="prefix">The prefix, including <c>=</c>.</param>
    /// <returns>The value, or empty when absent.</returns>
    private static string ReadArgument(string prefix)
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(prefix, StringComparison.Ordinal))
            {
                return argument[prefix.Length..];
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Formats a failure for the report.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <returns>The message, plus the exception type and stack when it wasn't an assertion.</returns>
    private static string Describe(Exception exception) =>
        exception is GodotTestFailedException
            ? exception.Message
            : $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}";

    /// <summary>
    /// Writes a JUnit XML report.
    /// </summary>
    /// <param name="path">The file path (relative to the working directory, or <c>res://</c>/<c>user://</c>).</param>
    /// <param name="results">The results.</param>
    private static void WriteJUnit(string path, IReadOnlyList<TestResult> results)
    {
        CultureInfo c = CultureInfo.InvariantCulture;
        var suite = new XElement(
            "testsuite",
            new XAttribute("name", "godot-runtime"),
            new XAttribute("tests", results.Count),
            new XAttribute("failures", results.Count(result => result.Failure is not null)),
            new XAttribute("time", results.Sum(result => result.Seconds).ToString("F3", c))
        );
        foreach (TestResult result in results)
        {
            var testCase = new XElement(
                "testcase",
                new XAttribute("classname", result.ClassName),
                new XAttribute("name", result.Name),
                new XAttribute("time", result.Seconds.ToString("F3", c))
            );
            if (result.Failure is { } failure)
            {
                testCase.Add(new XElement("failure", new XAttribute("message", failure), failure));
            }

            suite.Add(testCase);
        }

        string fullPath = path.Contains("://", StringComparison.Ordinal)
            ? ProjectSettings.GlobalizePath(path)
            : Path.GetFullPath(path);
        new XDocument(new XElement("testsuites", suite)).Save(fullPath);
    }

    /// <summary>
    /// Runs every discovered test, reports, and quits with the result as the exit code.
    /// </summary>
    /// <returns>A task that completes when the run is reported.</returns>
    private async Task RunAllAsync()
    {
        var capture = new EngineErrorCapture();
        OS.AddLogger(capture);
        List<MethodInfo> tests = Discover(ReadArgument(FilterPrefix));
        var results = new List<TestResult>(tests.Count);
        foreach (MethodInfo test in tests)
        {
            TestResult result = await RunOneAsync(test, capture);
            results.Add(result);
            _log.Info(
                LogCategory,
                result.Failure is null ? $"PASS {FullName(test)}" : $"FAIL {FullName(test)}: {result.Failure}"
            );
        }

        int failed = results.Count(result => result.Failure is not null);
        _log.Info(LogCategory, $"{results.Count - failed} passed, {failed} failed, {results.Count} total.");
        string junit = ReadArgument(JUnitPrefix);
        if (junit.Length > 0)
        {
            WriteJUnit(junit, results);
        }

        OS.RemoveLogger(capture);
        GetTree().Quit(failed > 0 || results.Count == 0 ? 1 : 0);
    }

    /// <summary>
    /// Runs one test in a fresh container node and collects its outcome.
    /// </summary>
    /// <param name="test">The test method.</param>
    /// <param name="capture">The engine error capture.</param>
    /// <returns>The result.</returns>
    private async Task<TestResult> RunOneAsync(MethodInfo test, EngineErrorCapture capture)
    {
        var container = new Node { Name = test.Name };
        AddChild(container);
        var context = new GodotTestContext(container);
        capture.Take();
        var stopwatch = Stopwatch.StartNew();
        string? failure = null;
        try
        {
            object? suite = test.IsStatic ? null : Activator.CreateInstance(test.DeclaringType ?? typeof(object));
            object?[]? arguments = test.GetParameters().Length == 1 ? [context] : null;
            if (test.Invoke(suite, arguments) is Task task)
            {
                // One timer raced against the test, rather than a per-frame poll: the runner must not
                // allocate while a test measures allocations.
                double timeout = test.GetCustomAttribute<GodotTestAttribute>()?.TimeoutSeconds ?? 0;
                SceneTreeTimer timer = GetTree().CreateTimer(timeout, processAlways: true, ignoreTimeScale: true);
                if (await Task.WhenAny(task, WaitForTimerAsync(timer)) != task)
                {
                    throw new TimeoutException($"Still running after {timeout} s.");
                }

                await task;
            }
        }
        catch (TargetInvocationException exception) when (exception.InnerException is { } inner)
        {
            failure = Describe(inner);
        }
        catch (Exception exception)
            when (exception
                    is GodotTestFailedException
                        or TimeoutException
                        or InvalidOperationException
                        or ArgumentException
            )
        {
            failure = Describe(exception);
        }

        // One more frame, so deferred calls and frees run while their errors still count against this test.
        container.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        string[] errors = capture.Take();
        if (failure is null && errors.Length > 0)
        {
            failure = "Godot logged errors: " + string.Join(" | ", errors);
        }

        return new TestResult(test.DeclaringType?.Name ?? "?", test.Name, failure, stopwatch.Elapsed.TotalSeconds);
    }

    /// <summary>
    /// Waits for a timer to fire.
    /// </summary>
    /// <param name="timer">The timer.</param>
    /// <returns>A task that completes when the timer times out.</returns>
    private async Task WaitForTimerAsync(SceneTreeTimer timer) =>
        await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);

    /// <summary>
    /// One test's outcome.
    /// </summary>
    /// <param name="ClassName">The test class.</param>
    /// <param name="Name">The test method.</param>
    /// <param name="Failure">Why it failed, or <see langword="null"/> when it passed.</param>
    /// <param name="Seconds">How long it took.</param>
    private sealed record TestResult(string ClassName, string Name, string? Failure, double Seconds);
}
