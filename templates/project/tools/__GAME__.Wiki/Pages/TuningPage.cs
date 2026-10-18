using System.Reflection;
using __GAME__.Core.Content;
using __GAME__.Wiki.Markdown;

namespace __GAME__.Wiki.Pages;

/// <summary>
/// Every tuning record in Core, found by reflection: each public type named <c>…Settings</c> and each of its static
/// instances (<c>MovementSettings.Default</c>). Values come from the compiled code, so they are the numbers the game
/// runs with, including the ones that don't live in content yet.
/// </summary>
internal static class TuningPage
{
    /// <summary>The navigation group of the reference pages.</summary>
    public const string Group = "Reference";

    private const string Page = "tuning.md";

    /// <summary>
    /// Gets every settings type and its static instances, by namespace, then name.
    /// </summary>
    /// <returns>The instances.</returns>
    public static IEnumerable<(Type Type, PropertyInfo Instance)> Instances() =>
        typeof(ContentDatabase)
            .Assembly.GetExportedTypes()
            .Where(type => type.Name.EndsWith("Settings", StringComparison.Ordinal) && !type.IsAbstract)
            .OrderBy(type => type.Namespace, StringComparer.Ordinal)
            .ThenBy(type => type.Name, StringComparer.Ordinal)
            .SelectMany(type =>
                type.GetProperties(BindingFlags.Public | BindingFlags.Static)
                    .Where(property => property.PropertyType == type)
                    .OrderBy(property => property.MetadataToken)
                    .Select(property => (type, property))
            );

    /// <summary>
    /// Writes the tuning page.
    /// </summary>
    /// <param name="context">The wiki context.</param>
    /// <returns>The page.</returns>
    public static IEnumerable<GeneratedPage> Write(WikiContext context)
    {
        MarkdownWriter writer = context.StartPage("Tuning", "src/");
        writer.Paragraph(
            "Every tuning record in Core: each public type named "
                + MarkdownWriter.Code("…Settings")
                + " and its named static instances, read from the compiled code. Tunable numbers belong in "
                + MarkdownWriter.Code("content/")
                + " (CLAUDE.md, architecture rules); until they move there, this is where they live."
        );

        string? folder = null;
        bool any = false;
        foreach ((Type type, PropertyInfo instance) in Instances())
        {
            any = true;
            string typeFolder = WikiContext.CoreFolder(type);
            if (typeFolder != folder)
            {
                folder = typeFolder;
                writer.Heading(2, MarkdownWriter.Code(folder));
            }

            string name = type.Name + "." + instance.Name;
            writer.Heading(3, name, MarkdownWriter.AnchorId(name));
            writer.Paragraph(
                string.Join(
                    " ",
                    new[] { context.Docs.Summary(type), context.Describe(instance) }.Where(text => text is not null)
                )
                    + " Source: "
                    + context.Source(typeFolder + type.Name + ".cs")
                    + "."
            );

            if (instance.GetValue(null) is { } value)
            {
                ObjectTable.Write(writer, context, value, 4);
            }
        }

        if (!any)
        {
            writer.Paragraph(
                "No settings records yet. A public record named "
                    + MarkdownWriter.Code("…Settings")
                    + " with a static instance (such as "
                    + MarkdownWriter.Code("Default")
                    + ") in "
                    + MarkdownWriter.Code(WikiContext.CoreRoot)
                    + " shows up here."
            );
        }

        yield return new GeneratedPage(Page, "Tuning", Group, writer.ToString());
    }
}
