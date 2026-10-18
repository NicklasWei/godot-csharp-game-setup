using __GAME__.Core.Content;
using __GAME__.Wiki.Markdown;

namespace __GAME__.Wiki.Pages;

/// <summary>
/// The glossary: every public enum in Core, by namespace, with each member's content name (the camelCase spelling
/// content files use) and its XML summary. It is the vocabulary of content and code in one place.
/// </summary>
internal static class GlossaryPage
{
    private const string Page = "glossary.md";

    /// <summary>
    /// Writes the glossary.
    /// </summary>
    /// <param name="context">The wiki context.</param>
    /// <returns>The page.</returns>
    public static IEnumerable<GeneratedPage> Write(WikiContext context)
    {
        MarkdownWriter writer = context.StartPage("Glossary", "src/");
        writer.Paragraph(
            "Every public enum in Core. Content files spell members by their content name, in camelCase. Names are as "
                + "permanent as content IDs once content or saves use them: add new ones, never rename."
        );

        Type[] enums =
        [
            .. typeof(ContentDatabase)
                .Assembly.GetExportedTypes()
                .Where(type => type.IsEnum)
                .OrderBy(type => type.Namespace, StringComparer.Ordinal)
                .ThenBy(type => type.Name, StringComparer.Ordinal),
        ];
        string? folder = null;
        foreach (Type type in enums)
        {
            string typeFolder = WikiContext.CoreFolder(type);
            if (typeFolder != folder)
            {
                folder = typeFolder;
                writer.Heading(2, MarkdownWriter.Code(folder));
            }

            writer.Heading(3, type.Name, MarkdownWriter.AnchorId("enum." + type.Name));
            writer.Paragraph(context.Docs.Summary(type));
            writer.Table(
                ["Member", "Content name", "Description"],
                Enum.GetValues(type)
                    .Cast<Enum>()
                    .Select(member =>
                        MarkdownWriter.Row(
                            MarkdownWriter.Code(member.ToString()),
                            MarkdownWriter.Code(ValueFormatter.EnumName(member)),
                            Describe(context, type, member)
                        )
                    )
            );
        }

        yield return new GeneratedPage(Page, "Glossary", TuningPage.Group, writer.ToString());
    }

    /// <summary>
    /// Gets an enum member's description, reporting a missing one.
    /// </summary>
    /// <param name="context">The wiki context.</param>
    /// <param name="type">The enum type.</param>
    /// <param name="member">The member.</param>
    /// <returns>The summary, or <see cref="ValueFormatter.None"/>.</returns>
    private static string Describe(WikiContext context, Type type, Enum member)
    {
        string? summary = context.Docs.Summary(member);
        if (summary is null)
        {
            context.Report("No XML doc summary: " + type.FullName + "." + member);
        }

        return summary ?? ValueFormatter.None;
    }
}
