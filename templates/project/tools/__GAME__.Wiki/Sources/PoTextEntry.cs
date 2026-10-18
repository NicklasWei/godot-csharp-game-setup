namespace __GAME__.Wiki.Sources;

/// <summary>
/// One entry of a PO catalog, as the wiki needs it.
/// </summary>
/// <param name="Text">The translation (the first plural form for plural entries); empty when untranslated.</param>
/// <param name="IsFuzzy">Whether the entry carries the fuzzy flag (untranslated or needs review).</param>
internal sealed record PoTextEntry(string Text, bool IsFuzzy)
{
    /// <summary>Gets a value indicating whether the entry has a translation that isn't flagged for review.</summary>
    public bool IsDone => Text.Length > 0 && !IsFuzzy;
}
