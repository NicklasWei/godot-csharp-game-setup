using System.Text;

namespace __GAME__.Wiki.Sources;

/// <summary>
/// One PO catalog from <c>localization/</c>: key msgids mapped to their translation and fuzzy flag. <c>en.po</c> is
/// the source of truth for player-facing names; the other catalogs feed the localization status page.
/// </summary>
internal sealed class PoText
{
    private readonly Dictionary<string, PoTextEntry> _entries;

    /// <summary>
    /// Initializes a new instance of the <see cref="PoText"/> class.
    /// </summary>
    /// <param name="locale">The locale code, from the file name.</param>
    /// <param name="entries">The entries keyed by msgid, header excluded.</param>
    private PoText(string locale, Dictionary<string, PoTextEntry> entries)
    {
        Locale = locale;
        _entries = entries;
    }

    /// <summary>Gets the locale code, e.g. <c>pt_BR</c>.</summary>
    public string Locale { get; }

    /// <summary>Gets every key, in file order.</summary>
    public IReadOnlyCollection<string> Keys => _entries.Keys;

    /// <summary>
    /// Reads a PO file. Plural entries keep their first form; the header is left out.
    /// </summary>
    /// <param name="path">The file path; its name is the locale.</param>
    /// <returns>The catalog.</returns>
    public static PoText Load(string path)
    {
        var entries = new Dictionary<string, PoTextEntry>(StringComparer.Ordinal);
        string? field = null;
        bool fuzzy = false;
        var id = new StringBuilder();
        var text = new StringBuilder();

        foreach (string raw in File.ReadLines(path, Encoding.UTF8))
        {
            string line = raw.Trim();
            if (line.Length == 0)
            {
                Add(entries, id, text, ref fuzzy);
                field = null;
            }
            else if (line.StartsWith("#,", StringComparison.Ordinal))
            {
                fuzzy |= line.Contains("fuzzy", StringComparison.Ordinal);
            }
            else if (line.StartsWith("msgid ", StringComparison.Ordinal))
            {
                if (id.Length > 0)
                {
                    Add(entries, id, text, ref fuzzy);
                }

                field = "msgid";
                id.Append(Unquote(line["msgid ".Length..]));
            }
            else if (line.StartsWith("msgstr ", StringComparison.Ordinal))
            {
                field = "msgstr";
                text.Append(Unquote(line["msgstr ".Length..]));
            }
            else if (line.StartsWith("msgstr[0] ", StringComparison.Ordinal))
            {
                field = "msgstr";
                text.Append(Unquote(line["msgstr[0] ".Length..]));
            }
            else if (line.StartsWith('"'))
            {
                (
                    field == "msgid" ? id
                    : field == "msgstr" ? text
                    : null
                )?.Append(Unquote(line));
            }
            else if (line.StartsWith("msg", StringComparison.Ordinal))
            {
                // msgid_plural or msgstr[n > 0]: the wiki shows the first form only.
                field = null;
            }
        }

        Add(entries, id, text, ref fuzzy);
        return new PoText(Path.GetFileNameWithoutExtension(path), entries);
    }

    /// <summary>
    /// Gets the text of a key.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="text">The text, when the key exists and is translated.</param>
    /// <returns>Whether the key has a non-empty translation.</returns>
    public bool TryGet(string key, out string text)
    {
        text = _entries.TryGetValue(key, out PoTextEntry? entry) ? entry.Text : string.Empty;
        return text.Length > 0;
    }

    /// <summary>
    /// Gets an entry.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <returns>The entry, or <see langword="null"/> when the catalog lacks the key.</returns>
    public PoTextEntry? Entry(string key) => _entries.GetValueOrDefault(key);

    /// <summary>
    /// Stores the entry read so far, if it has a key, then clears the buffers and the flag.
    /// </summary>
    /// <param name="entries">The collected entries.</param>
    /// <param name="id">The msgid buffer.</param>
    /// <param name="text">The msgstr buffer.</param>
    /// <param name="fuzzy">The fuzzy flag of the entry.</param>
    private static void Add(
        Dictionary<string, PoTextEntry> entries,
        StringBuilder id,
        StringBuilder text,
        ref bool fuzzy
    )
    {
        if (id.Length > 0)
        {
            entries[id.ToString()] = new PoTextEntry(text.ToString(), fuzzy);
        }

        id.Clear();
        text.Clear();
        fuzzy = false;
    }

    /// <summary>
    /// Removes the quotes around a PO string and resolves its escapes.
    /// </summary>
    /// <param name="quoted">The quoted string.</param>
    /// <returns>The content, or empty for a malformed string.</returns>
    private static string Unquote(string quoted) =>
        quoted.Length < 2 || quoted[0] != '"' || quoted[^1] != '"'
            ? string.Empty
            : quoted[1..^1]
                .Replace("\\n", "\n", StringComparison.Ordinal)
                .Replace("\\t", "\t", StringComparison.Ordinal)
                .Replace("\\\"", "\"", StringComparison.Ordinal)
                .Replace("\\\\", "\\", StringComparison.Ordinal);
}
