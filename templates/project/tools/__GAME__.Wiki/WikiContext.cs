using System.Reflection;
using __GAME__.Core.Content;
using __GAME__.Wiki.Markdown;
using __GAME__.Wiki.Sources;

namespace __GAME__.Wiki;

/// <summary>
/// What every page writer reads from: the content database the game loads, the PO catalogs, Core's XML docs, and the
/// build's commit. It also collects the problems a strict run fails on.
/// </summary>
internal sealed class WikiContext
{
    /// <summary>The source-of-truth locale.</summary>
    public const string SourceLocale = "en";

    private readonly List<string> _problems = [];
    private readonly HashSet<string> _undocumented = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="WikiContext"/> class.
    /// </summary>
    /// <param name="options">The run's options.</param>
    /// <param name="database">The loaded content.</param>
    /// <param name="catalogs">Every PO catalog, English first.</param>
    /// <param name="docs">Core's XML docs.</param>
    public WikiContext(WikiOptions options, ContentDatabase database, IReadOnlyList<PoText> catalogs, XmlDocs docs)
    {
        Options = options;
        Database = database;
        Catalogs = catalogs;
        Strings =
            catalogs.FirstOrDefault(catalog => catalog.Locale == SourceLocale)
            ?? throw new InvalidOperationException("localization/en.po is missing.");
        Docs = docs;
    }

    /// <summary>Gets the Core project's folder, e.g. <c>src/__GAME__.Core/</c>.</summary>
    public static string CoreRoot => "src/" + typeof(ContentDatabase).Assembly.GetName().Name + "/";

    /// <summary>Gets the run's options.</summary>
    public WikiOptions Options { get; }

    /// <summary>Gets the content, loaded by the same <see cref="ContentLoader"/> the game uses.</summary>
    public ContentDatabase Database { get; }

    /// <summary>Gets every PO catalog, English first.</summary>
    public IReadOnlyList<PoText> Catalogs { get; }

    /// <summary>Gets the English strings from <c>localization/en.po</c>.</summary>
    public PoText Strings { get; }

    /// <summary>Gets Core's XML docs.</summary>
    public XmlDocs Docs { get; }

    /// <summary>Gets the problems found so far: missing strings or docs, and content errors.</summary>
    public IReadOnlyList<string> Problems => _problems;

    /// <summary>
    /// Gets the repository folder of a Core namespace: <c>__GAME__.Core.Content</c> lives in
    /// <c>src/__GAME__.Core/Content/</c>.
    /// </summary>
    /// <param name="type">A Core type.</param>
    /// <returns>The folder path, ending in <c>/</c>.</returns>
    public static string CoreFolder(Type type)
    {
        string root = typeof(ContentDatabase).Assembly.GetName().Name ?? string.Empty;
        string ns = type.Namespace ?? root;
        string relative = ns.Length > root.Length ? ns[(root.Length + 1)..].Replace('.', '/') + "/" : string.Empty;
        return CoreRoot + relative;
    }

    /// <summary>
    /// Records a problem; a strict run fails if there are any.
    /// </summary>
    /// <param name="problem">What is wrong, and where.</param>
    public void Report(string problem) => _problems.Add(problem);

    /// <summary>
    /// Gets the English text of a localization key, reporting a missing one.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The text, or the key in code format when it is missing.</returns>
    public string Text(string key)
    {
        if (Strings.TryGet(key, out string text))
        {
            return text;
        }

        Report("Missing en.po string: " + key);
        return MarkdownWriter.Code(key);
    }

    /// <summary>
    /// Gets a property's XML summary, reporting a missing one on a Core type (once per property).
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The summary, or <see langword="null"/>.</returns>
    public string? Describe(PropertyInfo property)
    {
        string? summary = Docs.Summary(property);
        RequireDoc(summary, property.DeclaringType, property.Name);
        return summary;
    }

    /// <summary>
    /// Gets a field's or constant's XML summary, reporting a missing one on a Core type (once per field).
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>The summary, or <see langword="null"/>.</returns>
    public string? Describe(FieldInfo field)
    {
        string? summary = Docs.Summary(field);
        RequireDoc(summary, field.DeclaringType, field.Name);
        return summary;
    }

    /// <summary>
    /// Formats a repository file as a link to it on the code host, or as plain code when no source URL was given.
    /// </summary>
    /// <param name="repoPath">The path from the repository root, with forward slashes.</param>
    /// <returns>The Markdown.</returns>
    public string Source(string repoPath) =>
        Options.SourceUrl is null
            ? MarkdownWriter.Code(repoPath)
            : MarkdownWriter.Link(MarkdownWriter.Code(repoPath), Options.SourceUrl + repoPath);

    /// <summary>
    /// Starts a generated page: its title, then a note naming its sources and the commit.
    /// </summary>
    /// <param name="title">The page title.</param>
    /// <param name="sources">The repository paths the page is generated from.</param>
    /// <returns>A writer holding the header.</returns>
    public MarkdownWriter StartPage(string title, params IEnumerable<string> sources)
    {
        var writer = new MarkdownWriter().Heading(1, title);
        writer.Note(
            "Generated by "
                + MarkdownWriter.Code("tools/__GAME__.Wiki")
                + " from "
                + string.Join(", ", sources.Select(Source))
                + " at commit "
                + MarkdownWriter.Code(Options.Commit)
                + ". Edit the source, not this page."
        );
        return writer;
    }

    /// <summary>
    /// Reports a missing summary on a Core member, once per member.
    /// </summary>
    /// <param name="summary">The summary found, or <see langword="null"/>.</param>
    /// <param name="owner">The declaring type.</param>
    /// <param name="name">The member name.</param>
    private void RequireDoc(string? summary, Type? owner, string name)
    {
        bool fromCore = owner?.Assembly == typeof(ContentDatabase).Assembly;
        if (summary is null && fromCore && _undocumented.Add(owner?.FullName + "." + name))
        {
            Report("No XML doc summary: " + owner?.FullName + "." + name);
        }
    }
}
