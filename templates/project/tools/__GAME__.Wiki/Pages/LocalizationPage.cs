using __GAME__.Wiki.Markdown;
using __GAME__.Wiki.Sources;

namespace __GAME__.Wiki.Pages;

/// <summary>
/// The localization status page: per locale, how many keys are translated, and every key with its English text and
/// its state in each locale. <c>en.po</c> defines which keys exist.
/// </summary>
internal static class LocalizationPage
{
    private const string Page = "localization.md";

    /// <summary>
    /// Writes the page.
    /// </summary>
    /// <param name="context">The wiki context.</param>
    /// <returns>The page.</returns>
    public static IEnumerable<GeneratedPage> Write(WikiContext context)
    {
        MarkdownWriter writer = context.StartPage("Localization", "localization/");
        PoText english = context.Strings;
        PoText[] translated = [.. context.Catalogs.Where(catalog => catalog != english)];
        writer.Paragraph(
            "Keys are stable IDs, and "
                + MarkdownWriter.Code("en.po")
                + " is the source of truth. Untranslated entries keep an empty translation and the fuzzy flag, and the "
                + "game falls back to English for them."
        );

        writer.Heading(2, "Status");
        writer.Table(
            ["Locale", "Done", "Fuzzy or empty", "Missing"],
            context.Catalogs.Select(catalog =>
            {
                int done = english.Keys.Count(key => catalog.Entry(key)?.IsDone == true);
                int missing = english.Keys.Count(key => catalog.Entry(key) is null);
                return MarkdownWriter.Row(
                    MarkdownWriter.Code(catalog.Locale),
                    ValueFormatter.Number(done) + " / " + ValueFormatter.Number(english.Keys.Count),
                    ValueFormatter.Number(english.Keys.Count - done - missing),
                    ValueFormatter.Number(missing)
                );
            })
        );

        writer.Heading(2, "Keys");
        writer.Table(
            ["Key", "English", .. translated.Select(catalog => catalog.Locale)],
            english.Keys.Select(key =>
                MarkdownWriter.Row([
                    MarkdownWriter.Code(key),
                    context.Text(key),
                    .. translated.Select(catalog => State(catalog.Entry(key))),
                ])
            )
        );

        yield return new GeneratedPage(Page, "Localization", TuningPage.Group, writer.ToString());
    }

    /// <summary>
    /// Describes one entry's state in a translated locale.
    /// </summary>
    /// <param name="entry">The entry, or <see langword="null"/> when the catalog lacks the key.</param>
    /// <returns><c>done</c>, <c>fuzzy</c>, or <c>missing</c>.</returns>
    private static string State(PoTextEntry? entry) =>
        entry is null ? "missing"
        : entry.IsDone ? "done"
        : "fuzzy";
}
