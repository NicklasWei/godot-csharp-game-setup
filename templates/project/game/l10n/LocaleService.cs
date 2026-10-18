using System.Globalization;
using __GAME__.Core.Development;
using __GAME__.Core.Diagnostics;
using Godot;

namespace __GAME__.Game.L10n;

/// <summary>
/// Owns the active locale: translating keys, formatting localized strings with the right culture, and development
/// switches such as pseudolocalization.
/// </summary>
/// <remarks>
/// Translation data comes from the PO files registered in <c>project.godot</c>. Keys are defined in
/// <see cref="Core.Localization.L10nKeys"/> (Coding Standards §10, ADR 0005).
/// </remarks>
public sealed class LocaleService
{
    /// <summary>The log category used by this service.</summary>
    private const string LogCategory = "L10n";

    /// <summary>Destination for diagnostic messages.</summary>
    private readonly ILog _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocaleService"/> class, adopting the locale Godot selected at
    /// startup (the OS language, or the fallback locale).
    /// </summary>
    /// <param name="log">Destination for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException"><paramref name="log"/> is <see langword="null"/>.</exception>
    public LocaleService(ILog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        _log = log;
        Culture = ToCulture(TranslationServer.GetLocale());
    }

    /// <summary>
    /// Gets the .NET culture matching the active locale, used for number and date formatting. It is the invariant
    /// culture if .NET doesn't know the locale.
    /// </summary>
    public CultureInfo Culture { get; private set; }

    /// <summary>
    /// Translates a key into the active locale.
    /// </summary>
    /// <param name="key">A key from <see cref="Core.Localization.L10nKeys"/>.</param>
    /// <returns>The translated text, or the English text if the active locale lacks the entry.</returns>
    public static string Translate(string key) => TranslationServer.Translate(key).ToString();

    /// <summary>
    /// Switches the active locale for translations and formatting.
    /// </summary>
    /// <param name="godotLocale">A Godot locale code such as <c>en</c> or <c>pt_BR</c>.</param>
    /// <exception cref="ArgumentException"><paramref name="godotLocale"/> is null or empty.</exception>
    public void SetLocale(string godotLocale)
    {
        ArgumentException.ThrowIfNullOrEmpty(godotLocale);
        TranslationServer.SetLocale(godotLocale);
        Culture = ToCulture(TranslationServer.GetLocale());
        _log.Info(LogCategory, $"Locale set to '{TranslationServer.GetLocale()}'.");
    }

    /// <summary>
    /// Applies the development switches that concern localization.
    /// </summary>
    /// <param name="flags">The parsed dev flags.</param>
    /// <exception cref="ArgumentNullException"><paramref name="flags"/> is <see langword="null"/>.</exception>
    public void Apply(DevFlags flags)
    {
        ArgumentNullException.ThrowIfNull(flags);
        if (flags.Pseudolocalize)
        {
            TranslationServer.PseudolocalizationEnabled = true;
            _log.Info(LogCategory, "Pseudolocalization enabled.");
        }
    }

    /// <summary>
    /// Translates a key and fills its indexed placeholder using the active culture.
    /// </summary>
    /// <param name="key">A key whose text contains <c>{0}</c>.</param>
    /// <param name="argument">The value for <c>{0}</c>.</param>
    /// <returns>The formatted, translated text.</returns>
    public string Format(string key, object argument) => string.Format(Culture, Translate(key), argument);

    /// <summary>
    /// Translates a key and fills two indexed placeholders using the active culture.
    /// </summary>
    /// <param name="key">A key whose text contains <c>{0}</c> and <c>{1}</c>.</param>
    /// <param name="first">The value for <c>{0}</c>.</param>
    /// <param name="second">The value for <c>{1}</c>.</param>
    /// <returns>The formatted, translated text.</returns>
    public string Format(string key, object first, object second) =>
        string.Format(Culture, Translate(key), first, second);

    /// <summary>
    /// Maps a Godot locale code (<c>zh_CN</c>) to a .NET culture (<c>zh-CN</c>).
    /// </summary>
    /// <param name="godotLocale">The Godot locale code.</param>
    /// <returns>The matching culture, or <see cref="CultureInfo.InvariantCulture"/> if unknown.</returns>
    private static CultureInfo ToCulture(string godotLocale)
    {
        try
        {
            return CultureInfo.GetCultureInfo(godotLocale.Replace('_', '-'));
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }
}
