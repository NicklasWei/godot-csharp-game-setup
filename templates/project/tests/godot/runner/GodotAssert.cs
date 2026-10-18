using System.Diagnostics.CodeAnalysis;

namespace __GAME__.RuntimeTests.Runner;

/// <summary>
/// Assertions for Godot-runtime tests. A failed check throws <see cref="GodotTestFailedException"/>. These are
/// deliberately minimal: rules are tested in Core with xUnit, and runtime tests check wiring.
/// </summary>
public static class GodotAssert
{
    /// <summary>
    /// Fails unless the condition holds.
    /// </summary>
    /// <param name="condition">The condition.</param>
    /// <param name="message">What was expected.</param>
    /// <exception cref="GodotTestFailedException">The condition is false.</exception>
    public static void True([DoesNotReturnIf(false)] bool condition, string message)
    {
        if (!condition)
        {
            throw new GodotTestFailedException(message);
        }
    }

    /// <summary>
    /// Fails unless two values are equal.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <param name="what">A short name for the value, for the message.</param>
    /// <exception cref="GodotTestFailedException">The values differ.</exception>
    public static void Equal<T>(T expected, T actual, string what)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new GodotTestFailedException($"{what}: expected {expected}, got {actual}.");
        }
    }

    /// <summary>
    /// Fails when a value is <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="what">A short name for the value, for the message.</param>
    /// <exception cref="GodotTestFailedException">The value is <see langword="null"/>.</exception>
    public static void NotNull<T>([NotNull] T? value, string what)
        where T : class
    {
        if (value is null)
        {
            throw new GodotTestFailedException($"{what} is null.");
        }
    }

    /// <summary>
    /// Fails unconditionally.
    /// </summary>
    /// <param name="message">Why.</param>
    /// <exception cref="GodotTestFailedException">Always.</exception>
    [DoesNotReturn]
    public static void Fail(string message) => throw new GodotTestFailedException(message);
}
