using System.Collections;
using System.Reflection;

namespace __GAME__.Wiki.Markdown;

/// <summary>
/// Renders any Core object by reflection, so the wiki shows every field a definition or settings record has, with
/// its XML summary as the description, and picks up new fields without a generator change.
/// </summary>
/// <remarks>
/// Simple properties (<see cref="ValueFormatter.IsSimple"/>) go in a Property | Value | Description table. Each
/// complex one gets a sub-heading: a list becomes a table with one row per item, and a single object is rendered the
/// same way, one level deeper. Angles that Core stores in radians (<c>…Radians</c>) are shown in degrees.
/// </remarks>
internal static class ObjectTable
{
    private const string RadiansSuffix = "Radians";

    /// <summary>
    /// Gets a type's public instance properties, in declaration order.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The readable, non-indexer properties, without compiler-generated ones such as a record's <c>EqualityContract</c>.</returns>
    public static IReadOnlyList<PropertyInfo> Properties(Type type) =>
        [
            .. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.GetIndexParameters().Length == 0 && property.GetMethod is not null)
                .OrderBy(property => property.MetadataToken),
        ];

    /// <summary>
    /// Gets the name a property is shown under: <c>…Radians</c> becomes <c>…Degrees</c>.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The display name.</returns>
    public static string DisplayName(PropertyInfo property) =>
        IsRadians(property) ? property.Name[..^RadiansSuffix.Length] + "Degrees" : property.Name;

    /// <summary>
    /// Reads a property for display, converting radians to degrees.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="instance">The object.</param>
    /// <returns>The value to show.</returns>
    public static object? Read(PropertyInfo property, object instance)
    {
        object? value = property.GetValue(instance);
        return value is float radians && IsRadians(property) ? float.RadiansToDegrees(radians) : value;
    }

    /// <summary>
    /// Writes an object: its simple properties as a table, then each complex property under a sub-heading.
    /// </summary>
    /// <param name="writer">The page.</param>
    /// <param name="context">The wiki context, for descriptions.</param>
    /// <param name="instance">The object.</param>
    /// <param name="nestedLevel">The heading level for complex properties.</param>
    /// <param name="options">Rendering tweaks, or <see langword="null"/> for the defaults.</param>
    public static void Write(
        MarkdownWriter writer,
        WikiContext context,
        object instance,
        int nestedLevel,
        TableOptions? options = null
    )
    {
        options ??= TableOptions.Default;
        Type type = instance.GetType();
        object? defaults = options.SkipDefaults ? TryCreate(type) : null;
        var rows = new List<IReadOnlyList<string>>();
        var nested = new List<PropertyInfo>();

        foreach (PropertyInfo property in Properties(type))
        {
            if (options.Skip.Contains(property.Name))
            {
                continue;
            }

            object? value = Read(property, instance);
            if (defaults is not null && SameValue(value, Read(property, defaults)))
            {
                continue;
            }

            if (!ValueFormatter.IsSimple(property.PropertyType))
            {
                nested.Add(property);
                continue;
            }

            rows.Add([
                MarkdownWriter.Code(DisplayName(property)),
                options.Cell?.Invoke(property, value) ?? ValueFormatter.Format(value),
                Description(context, property),
            ]);
        }

        writer.Table(["Property", "Value", "Description"], rows);

        foreach (PropertyInfo property in nested)
        {
            object? value = property.GetValue(instance);
            if (value is null || (value is IEnumerable list && !list.GetEnumerator().MoveNext()))
            {
                continue;
            }

            writer.Heading(Math.Min(nestedLevel, 6), DisplayName(property));
            writer.Paragraph(context.Describe(property));
            if (value is IEnumerable items && ValueFormatter.ElementType(property.PropertyType) is { } element)
            {
                WriteRows(writer, context, items, element, "#", options);
            }
            else
            {
                Write(writer, context, value, nestedLevel + 1, options);
            }
        }
    }

    /// <summary>
    /// Writes a list as a table: an index column, then one column per simple property of the items, each header
    /// carrying the property's summary as a tooltip. Complex item properties are left out.
    /// </summary>
    /// <param name="writer">The page.</param>
    /// <param name="context">The wiki context, for descriptions.</param>
    /// <param name="items">The items.</param>
    /// <param name="elementType">The items' declared type.</param>
    /// <param name="indexHeader">The index column's header.</param>
    /// <param name="options">Rendering tweaks, or <see langword="null"/> for the defaults.</param>
    public static void WriteRows(
        MarkdownWriter writer,
        WikiContext context,
        IEnumerable items,
        Type elementType,
        string indexHeader,
        TableOptions? options = null
    )
    {
        options ??= TableOptions.Default;
        if (ValueFormatter.IsSimple(elementType))
        {
            writer.Paragraph(ValueFormatter.Format(items));
            return;
        }

        if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        {
            // A dictionary: one Key | Value row per entry.
            PropertyInfo key =
                elementType.GetProperty(nameof(KeyValuePair<,>.Key)) ?? throw new MissingMemberException();
            PropertyInfo value =
                elementType.GetProperty(nameof(KeyValuePair<,>.Value)) ?? throw new MissingMemberException();
            writer.Table(
                ["Key", "Value"],
                items
                    .Cast<object>()
                    .Select(pair =>
                        MarkdownWriter.Row(
                            ValueFormatter.Format(key.GetValue(pair)),
                            ValueFormatter.Format(value.GetValue(pair))
                        )
                    )
            );
            return;
        }

        List<PropertyInfo> columns =
        [
            .. Properties(elementType)
                .Where(property =>
                    !options.Skip.Contains(property.Name) && ValueFormatter.IsSimple(property.PropertyType)
                ),
        ];
        List<string> headers =
        [
            indexHeader,
            .. columns.Select(column =>
                MarkdownWriter.HeaderWithTooltip(DisplayName(column), context.Describe(column))
            ),
        ];

        var rows = new List<IReadOnlyList<string>>();
        int index = 0;
        foreach (object? item in items)
        {
            if (item is null)
            {
                continue;
            }

            var row = new List<string> { ValueFormatter.Number(index++) };
            foreach (PropertyInfo column in columns)
            {
                object? value = Read(column, item);
                row.Add(options.Cell?.Invoke(column, value) ?? ValueFormatter.Format(value));
            }

            rows.Add(row);
        }

        writer.Table(headers, rows);
    }

    /// <summary>
    /// Writes a type's fields without values: a Property | Description table, for authoring reference.
    /// </summary>
    /// <param name="writer">The page.</param>
    /// <param name="context">The wiki context, for descriptions.</param>
    /// <param name="type">The type.</param>
    public static void WriteFields(MarkdownWriter writer, WikiContext context, Type type) =>
        writer.Table(
            ["Property", "Description"],
            Properties(type)
                .Select(property =>
                    (IReadOnlyList<string>)[MarkdownWriter.Code(DisplayName(property)), Description(context, property)]
                )
        );

    /// <summary>
    /// Gets a property's description cell, noting a radians-to-degrees conversion.
    /// </summary>
    /// <param name="context">The wiki context.</param>
    /// <param name="property">The property.</param>
    /// <returns>The description, or <see cref="ValueFormatter.None"/>.</returns>
    private static string Description(WikiContext context, PropertyInfo property)
    {
        string summary = context.Describe(property) ?? ValueFormatter.None;
        return IsRadians(property) ? summary + " Shown in degrees." : summary;
    }

    /// <summary>
    /// Gets whether a property holds an angle in radians.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns><see langword="true"/> for a <see cref="float"/> named <c>…Radians</c>.</returns>
    private static bool IsRadians(PropertyInfo property) =>
        property.PropertyType == typeof(float) && property.Name.EndsWith(RadiansSuffix, StringComparison.Ordinal);

    /// <summary>
    /// Creates a default instance through a public parameterless constructor, to compare against.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The instance, or <see langword="null"/> when the type has no such constructor.</returns>
    private static object? TryCreate(Type type) =>
        type.IsValueType || type.GetConstructor(Type.EmptyTypes) is not null ? Activator.CreateInstance(type) : null;

    /// <summary>
    /// Compares two values, element by element for lists.
    /// </summary>
    /// <param name="left">One value.</param>
    /// <param name="right">The other.</param>
    /// <returns>Whether they are equal.</returns>
    private static bool SameValue(object? left, object? right) =>
        left is IEnumerable leftItems and not string && right is IEnumerable rightItems and not string
            ? leftItems.Cast<object?>().SequenceEqual(rightItems.Cast<object?>())
            : Equals(left, right);
}
