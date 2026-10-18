using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace __GAME__.Wiki.Markdown;

/// <summary>
/// Formats Core values for tables: numbers in the invariant culture, enums by their content names (camelCase, as
/// content files spell them), vectors as tuples, and lists of simple values joined by commas.
/// </summary>
internal static class ValueFormatter
{
    /// <summary>The text shown for an empty list or a missing value.</summary>
    public const string None = "—";

    /// <summary>
    /// Gets whether a type formats to one short cell, rather than needing a table of its own.
    /// </summary>
    /// <param name="type">The declared type.</param>
    /// <returns><see langword="true"/> for numbers, strings, enums, vectors, and lists of those.</returns>
    public static bool IsSimple(Type type)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (
            underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(string)
            || underlying == typeof(decimal)
            || underlying == typeof(TimeSpan)
            || underlying == typeof(Vector2)
            || underlying == typeof(Vector3)
        )
        {
            return true;
        }

        Type? element = ElementType(underlying);
        return element is not null && IsSimple(element);
    }

    /// <summary>
    /// Gets the element type of a list, or <see langword="null"/> when the type is not a list.
    /// </summary>
    /// <param name="type">The declared type.</param>
    /// <returns>The element type of <see cref="IEnumerable{T}"/>, if the type implements it (strings excluded).</returns>
    public static Type? ElementType(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        Type? enumerable =
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                ? type
                : Array.Find(
                    type.GetInterfaces(),
                    candidate =>
                        candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                );
        return enumerable?.GetGenericArguments()[0];
    }

    /// <summary>
    /// Formats a value for a table cell.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The Markdown text.</returns>
    public static string Format(object? value) =>
        value switch
        {
            null => None,
            string text => text.Length == 0 ? None : text,
            bool flag => flag ? "yes" : "no",
            float number => Number(number),
            double number => Number(number),
            TimeSpan span => Number(span.TotalSeconds) + " s",
            IFormattable formattable and not Enum and not Vector2 and not Vector3 => formattable.ToString(
                null,
                CultureInfo.InvariantCulture
            ),
            Enum member when IsEmptyFlags(member) => None,
            Enum member => EnumName(member),
            Vector2 vector => "(" + Number(vector.X) + ", " + Number(vector.Y) + ")",
            Vector3 vector => "(" + Number(vector.X) + ", " + Number(vector.Y) + ", " + Number(vector.Z) + ")",
            IEnumerable items => List(items),
            _ => value.ToString() ?? None,
        };

    /// <summary>
    /// Formats a number with up to four decimals and no trailing zeros.
    /// </summary>
    /// <param name="number">The number.</param>
    /// <returns>The text, e.g. <c>0.45</c> or <c>320</c>.</returns>
    public static string Number(double number) => number.ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats an enum member by its content name; flags enums list each set flag.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <returns>The camelCase name(s), e.g. <c>ironSword</c> or <c>fire, ice</c>.</returns>
    public static string EnumName(Enum member)
    {
        string[] names = member.ToString().Split(", ", StringSplitOptions.RemoveEmptyEntries);
        return string.Join(", ", names.Select(JsonNamingPolicy.CamelCase.ConvertName));
    }

    /// <summary>
    /// Splits a PascalCase name into title words: <c>SpawnTable</c> becomes <c>Spawn Table</c>.
    /// </summary>
    /// <param name="pascal">The name.</param>
    /// <returns>The words.</returns>
    public static string Words(string pascal)
    {
        var text = new StringBuilder(pascal.Length + 8);
        for (int i = 0; i < pascal.Length; i++)
        {
            if (i > 0 && char.IsUpper(pascal[i]) && !char.IsUpper(pascal[i - 1]))
            {
                text.Append(' ');
            }

            text.Append(pascal[i]);
        }

        return text.ToString();
    }

    /// <summary>
    /// Gets whether a value is the empty (zero) member of a flags enum, such as no tags.
    /// </summary>
    /// <param name="member">The value.</param>
    /// <returns><see langword="true"/> for a zero flags value.</returns>
    private static bool IsEmptyFlags(Enum member) =>
        member.GetType().IsDefined(typeof(FlagsAttribute), false)
        && Convert.ToInt64(member, CultureInfo.InvariantCulture) == 0;

    /// <summary>
    /// Joins the formatted items of a list.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <returns>The items separated by commas, or <see cref="None"/> for an empty list.</returns>
    private static string List(IEnumerable items)
    {
        var parts = new List<string>();
        foreach (object? item in items)
        {
            parts.Add(Format(item));
        }

        return parts.Count == 0 ? None : string.Join(", ", parts);
    }
}
