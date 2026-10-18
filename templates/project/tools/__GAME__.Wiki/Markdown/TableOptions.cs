using System.Reflection;

namespace __GAME__.Wiki.Markdown;

/// <summary>
/// Tweaks to how <see cref="ObjectTable"/> renders an object.
/// </summary>
internal sealed record TableOptions
{
    /// <summary>Gets the default options: every property, formatted by <see cref="ValueFormatter"/>.</summary>
    public static TableOptions Default { get; } = new();

    /// <summary>Gets the names of properties to leave out (a page shows them elsewhere).</summary>
    public IReadOnlySet<string> Skip { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets a value indicating whether properties equal to a freshly constructed instance's are left out, for records
    /// whose unused fields keep their defaults.
    /// </summary>
    public bool SkipDefaults { get; init; }

    /// <summary>
    /// Gets a custom cell formatter, given the property and its (converted) value; it returns <see langword="null"/> to
    /// fall back to <see cref="ValueFormatter.Format"/>. For index properties that should show a name.
    /// </summary>
    public Func<PropertyInfo, object?, string?>? Cell { get; init; }
}
