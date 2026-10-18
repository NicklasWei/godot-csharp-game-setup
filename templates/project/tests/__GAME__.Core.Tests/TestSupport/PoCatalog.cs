using System.Text;

namespace __GAME__.Core.Tests.TestSupport;

/// <summary>
/// A minimal gettext PO parser covering the subset this project uses: <c>msgid</c>,
/// <c>msgid_plural</c>, <c>msgstr</c>, <c>msgstr[n]</c>, multi-line strings, and the fuzzy flag.
/// </summary>
internal sealed class PoCatalog
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PoCatalog"/> class.
    /// </summary>
    /// <param name="locale">The locale code, taken from the file name.</param>
    /// <param name="entries">The non-header entries, keyed by msgid.</param>
    private PoCatalog(string locale, IReadOnlyDictionary<string, PoEntry> entries)
    {
        Locale = locale;
        Entries = entries;
    }

    /// <summary>Gets the locale code, e.g. <c>zh_CN</c>.</summary>
    public string Locale { get; }

    /// <summary>Gets the entries keyed by msgid (the localization key). The header is excluded.</summary>
    public IReadOnlyDictionary<string, PoEntry> Entries { get; }

    /// <summary>
    /// Loads <c>localization/&lt;locale&gt;.po</c>.
    /// </summary>
    /// <param name="locale">The locale code.</param>
    /// <returns>The parsed catalog.</returns>
    public static PoCatalog Load(string locale) =>
        Parse(locale, File.ReadAllLines(Path.Combine(RepoPaths.Localization, locale + ".po"), Encoding.UTF8));

    /// <summary>
    /// Parses PO lines.
    /// </summary>
    /// <param name="locale">The locale code.</param>
    /// <param name="lines">The file's lines.</param>
    /// <returns>The parsed catalog.</returns>
    /// <exception cref="FormatException">A line is malformed or a key is duplicated.</exception>
    public static PoCatalog Parse(string locale, IEnumerable<string> lines)
    {
        var entries = new Dictionary<string, PoEntry>(StringComparer.Ordinal);
        var builder = new EntryBuilder();
        string? currentField = null;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (line.Length == 0)
            {
                Flush(builder, entries);
                currentField = null;
                continue;
            }

            if (line.StartsWith("#,", StringComparison.Ordinal))
            {
                builder.IsFuzzy |= line.Contains("fuzzy", StringComparison.Ordinal);
                continue;
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('"'))
            {
                builder.Append(
                    currentField ?? throw new FormatException("String continuation without a field."),
                    Unquote(line)
                );
                continue;
            }

            int space = line.IndexOf(' ', StringComparison.Ordinal);
            if (space < 0)
            {
                throw new FormatException($"Malformed PO line: {line}");
            }

            currentField = line[..space];
            if (currentField == "msgid" && builder.HasMsgId)
            {
                Flush(builder, entries);
            }

            builder.Append(currentField, Unquote(line[(space + 1)..].Trim()));
        }

        Flush(builder, entries);
        return new PoCatalog(locale, entries);
    }

    /// <summary>
    /// Adds the entry being built to <paramref name="entries"/> (skipping the header) and resets the builder.
    /// </summary>
    /// <param name="builder">The entry under construction.</param>
    /// <param name="entries">The collected entries.</param>
    /// <exception cref="FormatException">The key is already present.</exception>
    private static void Flush(EntryBuilder builder, Dictionary<string, PoEntry> entries)
    {
        if (builder.HasMsgId && builder.MsgId.Length > 0)
        {
            PoEntry entry = builder.Build();
            if (!entries.TryAdd(entry.Key, entry))
            {
                throw new FormatException($"Duplicate PO key '{entry.Key}'.");
            }
        }

        builder.Reset();
    }

    /// <summary>
    /// Removes surrounding quotes and resolves PO escape sequences.
    /// </summary>
    /// <param name="quoted">A quoted PO string.</param>
    /// <returns>The unescaped content.</returns>
    /// <exception cref="FormatException">The value is not quoted.</exception>
    private static string Unquote(string quoted)
    {
        if (quoted.Length < 2 || quoted[0] != '"' || quoted[^1] != '"')
        {
            throw new FormatException($"Expected a quoted string: {quoted}");
        }

        return quoted[1..^1]
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\\t", "\t", StringComparison.Ordinal)
            .Replace("\\\"", "\"", StringComparison.Ordinal)
            .Replace("\\\\", "\\", StringComparison.Ordinal);
    }

    /// <summary>Accumulates the fields of one PO entry while parsing.</summary>
    private sealed class EntryBuilder
    {
        /// <summary>Field values keyed by field name (<c>msgid</c>, <c>msgstr[0]</c>, ...).</summary>
        private readonly Dictionary<string, StringBuilder> _fields = new(StringComparer.Ordinal);

        /// <summary>Gets or sets a value indicating whether the entry carries the fuzzy flag.</summary>
        public bool IsFuzzy { get; set; }

        /// <summary>Gets a value indicating whether a <c>msgid</c> has been read.</summary>
        public bool HasMsgId => _fields.ContainsKey("msgid");

        /// <summary>Gets the msgid read so far.</summary>
        public string MsgId => _fields.TryGetValue("msgid", out StringBuilder? value) ? value.ToString() : string.Empty;

        /// <summary>
        /// Appends text to a field.
        /// </summary>
        /// <param name="field">The field name.</param>
        /// <param name="text">The unescaped text.</param>
        public void Append(string field, string text)
        {
            if (!_fields.TryGetValue(field, out StringBuilder? value))
            {
                value = new StringBuilder();
                _fields[field] = value;
            }

            value.Append(text);
        }

        /// <summary>
        /// Creates the entry from the accumulated fields.
        /// </summary>
        /// <returns>The entry.</returns>
        public PoEntry Build()
        {
            List<string> translations =
            [
                .. _fields
                    .Where(pair => pair.Key.StartsWith("msgstr", StringComparison.Ordinal))
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => pair.Value.ToString()),
            ];
            string? pluralKey = _fields.TryGetValue("msgid_plural", out StringBuilder? plural)
                ? plural.ToString()
                : null;
            return new PoEntry(MsgId, pluralKey, translations, IsFuzzy);
        }

        /// <summary>Clears all state for the next entry.</summary>
        public void Reset()
        {
            _fields.Clear();
            IsFuzzy = false;
        }
    }
}
