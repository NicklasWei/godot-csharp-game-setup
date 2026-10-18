using System.Text;

namespace __GAME__.Wiki.Markdown;

/// <summary>
/// Builds one Markdown page for DocFX. Every block ends with a blank line, and the text always uses LF line endings,
/// so generated pages are byte-for-byte deterministic.
/// </summary>
internal sealed class MarkdownWriter
{
    private readonly StringBuilder _text = new();

    /// <summary>
    /// Escapes text for a table cell: pipes would split the cell and line breaks would end the row.
    /// </summary>
    /// <param name="text">The cell text, already Markdown.</param>
    /// <returns>The text, safe inside one cell.</returns>
    public static string Cell(string text) =>
        text.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

    /// <summary>
    /// Builds a table row.
    /// </summary>
    /// <param name="cells">The cells, already Markdown.</param>
    /// <returns>The row.</returns>
    public static IReadOnlyList<string> Row(params string[] cells) => cells;

    /// <summary>
    /// Formats inline code.
    /// </summary>
    /// <param name="text">The literal text; it must not contain a backtick.</param>
    /// <returns>The code span.</returns>
    public static string Code(string text) => "`" + text + "`";

    /// <summary>
    /// Formats a link.
    /// </summary>
    /// <param name="text">The link text, already Markdown.</param>
    /// <param name="href">The target: a relative page path, an anchor, or a URL.</param>
    /// <returns>The link.</returns>
    public static string Link(string text, string href) => "[" + text + "](" + href + ")";

    /// <summary>
    /// Turns an ID into an HTML anchor ID: <c>item.iron_sword</c> becomes <c>item-iron-sword</c>.
    /// </summary>
    /// <param name="id">A content ID or other dotted name.</param>
    /// <returns>The anchor ID, without the <c>#</c>.</returns>
    public static string AnchorId(string id) => id.Replace('.', '-').Replace('_', '-').ToLowerInvariant();

    /// <summary>
    /// Formats a column header with its description as a hover tooltip.
    /// </summary>
    /// <param name="name">The header text.</param>
    /// <param name="description">The tooltip, or <see langword="null"/> for none.</param>
    /// <returns>The header cell content.</returns>
    public static string HeaderWithTooltip(string name, string? description) =>
        description is null
            ? name
            : "<abbr title=\""
                + description.Replace("\"", "&quot;", StringComparison.Ordinal).Replace('`', '\'')
                + "\">"
                + name
                + "</abbr>";

    /// <summary>
    /// Writes a heading, optionally preceded by an explicit anchor that stays stable when the heading text changes.
    /// </summary>
    /// <param name="level">The heading level, 1 to 6.</param>
    /// <param name="text">The heading text.</param>
    /// <param name="anchorId">An anchor ID from <see cref="AnchorId"/>, or <see langword="null"/>.</param>
    /// <returns>This writer.</returns>
    public MarkdownWriter Heading(int level, string text, string? anchorId = null)
    {
        if (anchorId is not null)
        {
            _text.Append("<a id=\"").Append(anchorId).Append("\"></a>\n\n");
        }

        _text.Append('#', level).Append(' ').Append(text).Append("\n\n");
        return this;
    }

    /// <summary>
    /// Writes a paragraph; writes nothing for empty text.
    /// </summary>
    /// <param name="text">The paragraph, already Markdown.</param>
    /// <returns>This writer.</returns>
    public MarkdownWriter Paragraph(string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            _text.Append(text).Append("\n\n");
        }

        return this;
    }

    /// <summary>
    /// Writes a DocFX note callout.
    /// </summary>
    /// <param name="text">The note, already Markdown, on one line.</param>
    /// <returns>This writer.</returns>
    public MarkdownWriter Note(string text)
    {
        _text.Append("> [!NOTE]\n> ").Append(text).Append("\n\n");
        return this;
    }

    /// <summary>
    /// Writes a bullet list; writes nothing for no items.
    /// </summary>
    /// <param name="items">The items, already Markdown, one line each.</param>
    /// <returns>This writer.</returns>
    public MarkdownWriter Bullets(IEnumerable<string> items)
    {
        bool any = false;
        foreach (string item in items)
        {
            _text.Append("- ").Append(item).Append('\n');
            any = true;
        }

        if (any)
        {
            _text.Append('\n');
        }

        return this;
    }

    /// <summary>
    /// Writes a table; writes nothing for no rows.
    /// </summary>
    /// <param name="headers">The column headers.</param>
    /// <param name="rows">The rows, each with one cell per header, already Markdown.</param>
    /// <returns>This writer.</returns>
    public MarkdownWriter Table(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var lines = new StringBuilder();
        foreach (IReadOnlyList<string> row in rows)
        {
            lines.Append("| ").AppendJoin(" | ", row.Select(Cell)).Append(" |\n");
        }

        if (lines.Length == 0)
        {
            return this;
        }

        _text.Append("| ").AppendJoin(" | ", headers.Select(Cell)).Append(" |\n");
        _text.Append('|').AppendJoin('|', headers.Select(_ => "---")).Append("|\n");
        _text.Append(lines).Append('\n');
        return this;
    }

    /// <inheritdoc/>
    public override string ToString() => _text.ToString();
}
