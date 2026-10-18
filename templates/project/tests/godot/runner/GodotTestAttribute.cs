namespace __GAME__.RuntimeTests.Runner;

/// <summary>
/// Marks a method as a Godot-runtime test (ADR 0006). The method is public and usually static (an instance method
/// needs a parameterless constructor on its class), and takes either nothing or a <see cref="GodotTestContext"/>.
/// It returns <see langword="void"/>, or a <see cref="System.Threading.Tasks.Task"/> to await frames.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class GodotTestAttribute : Attribute
{
    /// <summary>Gets how long an asynchronous test may run, in real seconds, before it fails as timed out.</summary>
    public double TimeoutSeconds { get; init; } = 60;
}
