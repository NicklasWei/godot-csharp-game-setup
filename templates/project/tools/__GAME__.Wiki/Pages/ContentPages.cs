using System.Reflection;
using __GAME__.Core.Content;
using __GAME__.Wiki.Markdown;

namespace __GAME__.Wiki.Pages;

/// <summary>
/// The content pages: an index of every registered kind (<see cref="ContentKinds.All"/>), then one page per kind
/// with its authoring fields and every definition, rendered by reflection. A new kind or field appears without a
/// generator change; a kind with special needs can still get its own writer.
/// </summary>
internal static class ContentPages
{
    /// <summary>The navigation group of the content pages.</summary>
    public const string Group = "Content";

    /// <summary>The folder the content pages are written to, under the reference section.</summary>
    private const string Folder = "content";

    /// <summary>
    /// Writes the content pages.
    /// </summary>
    /// <param name="context">The wiki context.</param>
    /// <returns>The index page, then one page per kind.</returns>
    public static IEnumerable<GeneratedPage> Write(WikiContext context)
    {
        IReadOnlyList<IContentKind> kinds = ContentKinds.All;
        MarkdownWriter index = context.StartPage(
            "Content",
            WikiContext.CoreFolder(typeof(ContentKinds)) + "ContentKinds.cs",
            "content/"
        );
        index.Paragraph(context.Docs.Summary(typeof(ContentKinds)));
        if (kinds.Count == 0)
        {
            index.Paragraph(
                "No content kinds are registered yet. Add one to "
                    + MarkdownWriter.Code("ContentKinds.All")
                    + " and it gets a page here, listing its fields and every definition."
            );
        }

        index.Table(
            ["Kind", "Folder", "ID prefix", "Definitions", "Schema"],
            kinds.Select(kind =>
                MarkdownWriter.Row(
                    MarkdownWriter.Link(KindTitle(kind), PagePath(kind)[(Folder.Length + 1)..]),
                    MarkdownWriter.Code("content/" + kind.Folder + "/"),
                    MarkdownWriter.Code(kind.IdPrefix + "."),
                    ValueFormatter.Number(Definitions(context, kind).Count),
                    context.Source("content/schemas/" + kind.SchemaName + ".schema.json")
                )
            )
        );
        yield return new GeneratedPage(Folder + "/index.md", "Overview", Group, index.ToString());

        foreach (IContentKind kind in kinds)
        {
            yield return KindPage(context, kind);
        }
    }

    /// <summary>
    /// Writes one kind's page.
    /// </summary>
    /// <param name="context">The wiki context.</param>
    /// <param name="kind">The kind.</param>
    /// <returns>The page.</returns>
    private static GeneratedPage KindPage(WikiContext context, IContentKind kind)
    {
        MarkdownWriter writer = context.StartPage(
            KindTitle(kind),
            "content/" + kind.Folder + "/",
            WikiContext.CoreFolder(kind.DefinitionType)
        );
        writer.Paragraph(context.Docs.Summary(kind.DefinitionType));

        writer.Heading(2, "Fields");
        writer.Paragraph(
            "What a file in "
                + MarkdownWriter.Code("content/" + kind.Folder + "/")
                + " may contain. Its ID is "
                + MarkdownWriter.Code(kind.IdPrefix + ".<file name>")
                + ", and editors validate it against "
                + context.Source("content/schemas/" + kind.SchemaName + ".schema.json")
                + "."
        );
        writer.Table(
            ["Field", "Type", "Description"],
            kind.DocumentType.Properties.OrderBy(field => field.AttributeProvider is PropertyInfo { DeclaringType: var owner } && owner == typeof(ContentDocument) ? 0 : 1)
                .Select(field =>
                    MarkdownWriter.Row(
                        MarkdownWriter.Code(field.Name),
                        JsonTypeName(field.PropertyType),
                        field.AttributeProvider is PropertyInfo property
                            ? context.Describe(property) ?? ValueFormatter.None
                            : ValueFormatter.None
                    )
                )
        );

        writer.Heading(2, "Definitions");
        IReadOnlyList<IContentDefinition> definitions = Definitions(context, kind);
        if (definitions.Count == 0)
        {
            writer.Paragraph("None yet.");
        }

        foreach (IContentDefinition definition in definitions)
        {
            writer.Heading(3, MarkdownWriter.Code(definition.Id), MarkdownWriter.AnchorId(definition.Id));
            string[] keys = [.. kind.RequiredLocalizationKeys(definition)];
            writer.Table(
                ["Key", "English"],
                keys.Select(key => MarkdownWriter.Row(MarkdownWriter.Code(key), context.Text(key)))
            );
            writer.Paragraph("Source: " + context.Source(SourcePath(kind, definition)) + ".");
            ObjectTable.Write(writer, context, definition, 4, new TableOptions { Skip = new HashSet<string> { "Id" } });
        }

        return new GeneratedPage(PagePath(kind), KindTitle(kind), Group, writer.ToString());
    }

    /// <summary>
    /// Gets a kind's definitions, ordered by ID.
    /// </summary>
    /// <param name="context">The wiki context.</param>
    /// <param name="kind">The kind.</param>
    /// <returns>The definitions whose ID carries the kind's prefix and whose type is the kind's.</returns>
    private static IReadOnlyList<IContentDefinition> Definitions(WikiContext context, IContentKind kind) =>
        [
            .. context.Database.All.Where(definition =>
                kind.DefinitionType.IsInstanceOfType(definition)
                && definition.Id.StartsWith(kind.IdPrefix + ".", StringComparison.Ordinal)
            ),
        ];

    /// <summary>
    /// Names a document property's JSON type, as content authors see it.
    /// </summary>
    /// <param name="type">The property's .NET type.</param>
    /// <returns>The JSON type, e.g. <c>integer</c>, <c>array of string</c>, or <c>object</c>.</returns>
    private static string JsonTypeName(Type type)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(string))
        {
            return "string";
        }

        if (underlying == typeof(bool))
        {
            return "boolean";
        }

        if (underlying == typeof(float) || underlying == typeof(double) || underlying == typeof(decimal))
        {
            return "number";
        }

        if (underlying.IsPrimitive)
        {
            return "integer";
        }

        bool isDictionary = underlying.GetInterfaces()
            .Append(underlying)
            .Any(candidate =>
                candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>)
            );
        return !isDictionary && ValueFormatter.ElementType(underlying) is { } element
            ? "array of " + JsonTypeName(element)
            : "object";
    }

    /// <summary>
    /// Gets a kind's navigation title: its folder in title words, e.g. <c>spawn_tables</c> becomes <c>Spawn tables</c>.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The title.</returns>
    private static string KindTitle(IContentKind kind) =>
        char.ToUpperInvariant(kind.Folder[0]) + kind.Folder[1..].Replace('_', ' ');

    /// <summary>
    /// Gets a kind's page path under the reference section.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The path, e.g. <c>content/items.md</c>.</returns>
    private static string PagePath(IContentKind kind) => Folder + "/" + kind.Folder + ".md";

    /// <summary>
    /// Gets the file a definition came from.
    /// </summary>
    /// <param name="kind">The definition's kind.</param>
    /// <param name="definition">The definition.</param>
    /// <returns>The repository path.</returns>
    private static string SourcePath(IContentKind kind, IContentDefinition definition) =>
        "content/" + kind.Folder + "/" + definition.Id[(kind.IdPrefix.Length + 1)..] + ".json";
}
