namespace __GAME__.RuntimeTests.Runner;

/// <summary>
/// Thrown by <see cref="GodotAssert"/> when a runtime test's expectation fails.
/// </summary>
public sealed class GodotTestFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GodotTestFailedException"/> class.
    /// </summary>
    public GodotTestFailedException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GodotTestFailedException"/> class.
    /// </summary>
    /// <param name="message">What was expected and what happened.</param>
    public GodotTestFailedException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GodotTestFailedException"/> class.
    /// </summary>
    /// <param name="message">What was expected and what happened.</param>
    /// <param name="innerException">The underlying failure.</param>
    public GodotTestFailedException(string message, Exception innerException)
        : base(message, innerException) { }
}
