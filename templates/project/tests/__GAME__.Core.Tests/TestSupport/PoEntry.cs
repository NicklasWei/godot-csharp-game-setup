namespace __GAME__.Core.Tests.TestSupport;

/// <summary>
/// One parsed PO entry.
/// </summary>
/// <param name="Key">The msgid (localization key).</param>
/// <param name="PluralKey">The msgid_plural, if the entry has plural forms.</param>
/// <param name="Translations">The msgstr values (one per plural form).</param>
/// <param name="IsFuzzy">Whether the entry is flagged fuzzy (untranslated or needs review).</param>
internal sealed record PoEntry(string Key, string? PluralKey, IReadOnlyList<string> Translations, bool IsFuzzy)
{
    /// <summary>Gets a value indicating whether every translation form is non-empty.</summary>
    public bool IsTranslated => Translations.Count > 0 && Translations.All(text => text.Length > 0);
}
