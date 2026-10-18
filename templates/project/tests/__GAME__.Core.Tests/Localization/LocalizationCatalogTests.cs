using System.Reflection;
using System.Text.RegularExpressions;
using __GAME__.Core.Content;
using __GAME__.Core.Localization;
using __GAME__.Core.Tests.TestSupport;

namespace __GAME__.Core.Tests.Localization;

/// <summary>
/// Validates the real PO catalogs in <c>localization/</c> against <see cref="L10nKeys"/>, the content kinds' keys,
/// the game scenes, <c>project.godot</c>, and each other (Coding Standards §10, ADR 0005).
/// </summary>
public sealed partial class LocalizationCatalogTests
{
    /// <summary>The source-of-truth locale.</summary>
    private const string SourceLocale = "en";

    /// <summary>
    /// Gets the translated (non-source) locales: every other <c>.po</c> file in <c>localization/</c>. Adding a
    /// locale is adding its file (with every key, fuzzy) and registering it in <c>project.godot</c>.
    /// </summary>
    private static IReadOnlyList<string> TranslatedLocales =>
        [
            .. Directory
                .GetFiles(RepoPaths.Localization, "*.po")
                .Select(Path.GetFileNameWithoutExtension)
                .OfType<string>()
                .Where(locale => locale != SourceLocale)
                .Order(StringComparer.Ordinal),
        ];

    [Fact]
    public void L10nKeys_EveryConstant_ExistsInEnglishCatalog()
    {
        // Arrange
        PoCatalog english = PoCatalog.Load(SourceLocale);

        // Act
        string[] missing = [.. CodeKeys().Where(key => !english.Entries.ContainsKey(key))];

        // Assert
        Assert.Empty(missing);
    }

    [Fact]
    public void ContentKeys_EveryDefinition_HasItsKeysInEnglish()
    {
        // Arrange
        PoCatalog english = PoCatalog.Load(SourceLocale);

        // Act
        string[] missing = [.. ContentKeys().Where(key => !english.Entries.ContainsKey(key))];

        // Assert
        Assert.Empty(missing);
    }

    [Fact]
    public void EnglishCatalog_EveryKey_IsReferencedByCodeContentOrScenes()
    {
        // Arrange
        PoCatalog english = PoCatalog.Load(SourceLocale);
        var referenced = new HashSet<string>(CodeKeys(), StringComparer.Ordinal);
        referenced.UnionWith(ContentKeys());
        referenced.UnionWith(SceneTextScanner.FindTranslatableTexts().Select(text => text.Text));

        // Act
        string[] orphans = [.. english.Entries.Keys.Where(key => !referenced.Contains(key))];

        // Assert
        Assert.Empty(orphans);
    }

    [Fact]
    public void EnglishCatalog_EveryEntry_IsTranslatedAndNotFuzzy()
    {
        // Arrange
        PoCatalog english = PoCatalog.Load(SourceLocale);

        // Act
        string[] incomplete = [.. english.Entries.Values.Where(e => e.IsFuzzy || !e.IsTranslated).Select(e => e.Key)];

        // Assert
        Assert.Empty(incomplete);
    }

    [Fact]
    public void Scenes_EveryTranslatableText_IsKnownKey()
    {
        // Arrange
        PoCatalog english = PoCatalog.Load(SourceLocale);

        // Act
        string[] unknown =
        [
            .. SceneTextScanner
                .FindTranslatableTexts()
                .Where(text => !english.Entries.ContainsKey(text.Text))
                .Select(text => $"{text.ScenePath} :: {text.NodeName} :: \"{text.Text}\""),
        ];

        // Assert
        Assert.Empty(unknown);
    }

    [Fact]
    public void Catalogs_KeySets_MatchEnglish()
    {
        // Arrange
        PoCatalog english = PoCatalog.Load(SourceLocale);

        // Act
        string[] mismatches =
        [
            .. TranslatedLocales.SelectMany(locale =>
            {
                PoCatalog catalog = PoCatalog.Load(locale);
                return english
                    .Entries.Keys.Except(catalog.Entries.Keys, StringComparer.Ordinal)
                    .Select(key => $"{locale} lacks {key}")
                    .Concat(
                        catalog
                            .Entries.Keys.Except(english.Entries.Keys, StringComparer.Ordinal)
                            .Select(key => $"{locale} has extra {key}")
                    );
            }),
        ];

        // Assert
        Assert.Empty(mismatches);
    }

    [Fact]
    public void Catalogs_UntranslatedEntries_AreMarkedFuzzy()
    {
        // Act
        string[] unflagged =
        [
            .. TranslatedLocales.SelectMany(locale =>
                PoCatalog
                    .Load(locale)
                    .Entries.Values.Where(entry => !entry.IsTranslated && !entry.IsFuzzy)
                    .Select(entry => $"{locale}: {entry.Key}")
            ),
        ];

        // Assert
        Assert.Empty(unflagged);
    }

    [Fact]
    public void Catalogs_TranslatedEntries_KeepEnglishPlaceholders()
    {
        // Arrange
        PoCatalog english = PoCatalog.Load(SourceLocale);

        // Act
        string[] mismatched =
        [
            .. TranslatedLocales.SelectMany(locale =>
                PoCatalog
                    .Load(locale)
                    .Entries.Values.Where(entry => entry.IsTranslated && english.Entries.ContainsKey(entry.Key))
                    .Where(entry =>
                    {
                        string[] expected = Placeholders(english.Entries[entry.Key].Translations[0]);
                        return entry.Translations.Any(text => !expected.SequenceEqual(Placeholders(text)));
                    })
                    .Select(entry => $"{locale}: {entry.Key}")
            ),
        ];

        // Assert
        Assert.Empty(mismatched);
    }

    [Fact]
    public void ProjectSettings_RegisterEveryCatalog()
    {
        // Arrange
        string[] expected =
        [
            .. new[] { SourceLocale }.Concat(TranslatedLocales).Select(locale => $"res://localization/{locale}.po"),
        ];

        // Act
        IReadOnlyList<string> registered = GodotProjectFile.ReadTranslationPaths();

        // Assert
        Assert.Equal(expected.Order(StringComparer.Ordinal), registered.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// Reads every key constant declared on <see cref="L10nKeys"/>.
    /// </summary>
    /// <returns>The key values.</returns>
    private static IEnumerable<string> CodeKeys() =>
        typeof(L10nKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => field.GetRawConstantValue())
            .OfType<string>();

    /// <summary>
    /// Lists the keys the real content needs, as each registered kind declares them.
    /// </summary>
    /// <returns>The keys.</returns>
    private static IEnumerable<string> ContentKeys()
    {
        ContentDatabase content = RealContent.Load().Database;
        return ContentKinds.All.SelectMany(kind =>
            content
                .All.Where(definition => kind.DefinitionType.IsInstanceOfType(definition))
                .Where(definition => definition.Id.StartsWith(kind.IdPrefix + ".", StringComparison.Ordinal))
                .SelectMany(kind.RequiredLocalizationKeys)
        );
    }

    /// <summary>
    /// Extracts the sorted, distinct indexed placeholders (<c>{0}</c>, <c>{1}</c>) from a string.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <returns>The placeholders in ordinal order.</returns>
    private static string[] Placeholders(string text) =>
        [.. PlaceholderPattern().Matches(text).Select(m => m.Value).Distinct().Order(StringComparer.Ordinal)];

    /// <summary>Matches an indexed placeholder with optional format, e.g. <c>{0}</c> or <c>{1:N0}</c>.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\{\d+(:[^}]*)?\}")]
    private static partial Regex PlaceholderPattern();
}
