using System.Text;
using __GAME__.Core.Content;
using __GAME__.Wiki.Pages;
using __GAME__.Wiki.Sources;

namespace __GAME__.Wiki;

/// <summary>
/// Generates the wiki's Markdown: loads content with Core's <see cref="ContentLoader"/>, runs every page writer, and
/// writes the pages plus their <c>toc.yml</c> files into the output folder, replacing what a previous run left.
/// </summary>
/// <remarks>
/// Output layout: <c>reference/</c> (the generated pages and their TOC) and <c>docs/toc.yml</c> (the TOC of the
/// repository's <c>docs/</c>, which DocFX reads in place). <c>tools/wiki/docfx.json</c> maps both into the site.
/// Every page is derived from a registry in code (content kinds, settings records, enums, the input map, dev flags,
/// PO catalogs), so a new entry in any of them appears without a generator change.
/// </remarks>
internal static class WikiGenerator
{
    /// <summary>The file that marks a folder as generator output, so a run only ever deletes its own output.</summary>
    private const string Marker = ".wiki-generated";

    /// <summary>The page writers, in navigation order.</summary>
    private static readonly Func<WikiContext, IEnumerable<GeneratedPage>>[] Writers =
    [
        ContentPages.Write,
        TuningPage.Write,
        GlossaryPage.Write,
        ControlsPage.Write,
        DevFlagsPage.Write,
        LocalizationPage.Write,
    ];

    /// <summary>
    /// Runs the generator.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="output">Progress output.</param>
    /// <param name="errors">Problem output.</param>
    /// <returns>The process exit code: 0 on success, 1 on failure.</returns>
    public static int Run(WikiOptions options, TextWriter output, TextWriter errors)
    {
        XmlDocs? docs = XmlDocs.TryLoad(typeof(ContentDatabase).Assembly);
        if (docs is null)
        {
            errors.WriteLine("error: the Core XML docs are missing next to the Core assembly; build Core first.");
            return 1;
        }

        ContentLoadResult content = ContentLoader.Load(ReadContent(Path.Combine(options.RepoRoot, "content")));
        string localization = Path.Combine(options.RepoRoot, "localization");
        PoText[] catalogs =
        [
            .. Directory
                .GetFiles(localization, "*.po")
                .Order(StringComparer.Ordinal)
                .Select(PoText.Load)
                .OrderBy(catalog => catalog.Locale == WikiContext.SourceLocale ? 0 : 1),
        ];
        var context = new WikiContext(options, content.Database, catalogs, docs);
        foreach (ContentError error in content.Errors)
        {
            context.Report("Content error: " + error);
        }

        List<GeneratedPage> pages = [.. Writers.SelectMany(writer => writer(context))];

        if (!TryResetOutput(options.OutputDirectory, errors))
        {
            return 1;
        }

        foreach (GeneratedPage page in pages)
        {
            WriteFile(options.OutputDirectory, "reference/" + page.Path, page.Text);
        }

        WriteFile(options.OutputDirectory, "reference/toc.yml", TocWriter.Pages(pages));
        WriteFile(
            options.OutputDirectory,
            "docs/toc.yml",
            TocWriter.Docs(Path.Combine(options.RepoRoot, "docs"), Path.Combine(options.OutputDirectory, "docs"))
        );
        output.WriteLine("Wrote " + pages.Count + " pages to " + options.OutputDirectory + ".");

        foreach (string problem in context.Problems)
        {
            errors.WriteLine((options.Strict ? "error: " : "warning: ") + problem);
        }

        return content.IsValid && (!options.Strict || context.Problems.Count == 0) ? 0 : 1;
    }

    /// <summary>
    /// Reads every content file the way the game does: content-relative forward-slash paths, schemas left out.
    /// </summary>
    /// <param name="contentDirectory">The <c>content/</c> folder.</param>
    /// <returns>The files.</returns>
    private static List<ContentFile> ReadContent(string contentDirectory) =>
        Directory.Exists(contentDirectory)
            ?
            [
                .. Directory
                    .GetFiles(contentDirectory, "*", SearchOption.AllDirectories)
                    .Select(path => Path.GetRelativePath(contentDirectory, path).Replace('\\', '/'))
                    .Where(path => !path.StartsWith(ContentLoader.SchemaFolder + "/", StringComparison.Ordinal))
                    .Select(path => new ContentFile(path, File.ReadAllText(Path.Combine(contentDirectory, path)))),
            ]
            : [];

    /// <summary>
    /// Empties the output folder, refusing to touch a folder holding files that a previous run didn't create. The
    /// marker is kept while the rest is deleted, so a failed reset (a file held open) can be retried.
    /// </summary>
    /// <param name="directory">The output folder.</param>
    /// <param name="errors">Problem output.</param>
    /// <returns>Whether the folder is ready.</returns>
    private static bool TryResetOutput(string directory, TextWriter errors)
    {
        string marker = Path.Combine(directory, Marker);
        try
        {
            if (Directory.Exists(directory))
            {
                if (!File.Exists(marker) && Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Any())
                {
                    errors.WriteLine(
                        "error: " + directory + " holds files the generator didn't write; not clearing it."
                    );
                    return false;
                }

                foreach (string child in Directory.EnumerateDirectories(directory))
                {
                    Directory.Delete(child, recursive: true);
                }

                foreach (string file in Directory.EnumerateFiles(directory).Where(file => file != marker))
                {
                    File.Delete(file);
                }
            }

            Directory.CreateDirectory(directory);
            File.WriteAllText(marker, string.Empty);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            errors.WriteLine("error: could not clear " + directory + ": " + exception.Message);
            return false;
        }
    }

    /// <summary>
    /// Writes one file as UTF-8 without a byte order mark.
    /// </summary>
    /// <param name="root">The output folder.</param>
    /// <param name="relativePath">The path under it, with forward slashes.</param>
    /// <param name="text">The text.</param>
    private static void WriteFile(string root, string relativePath, string text)
    {
        string path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? root);
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
