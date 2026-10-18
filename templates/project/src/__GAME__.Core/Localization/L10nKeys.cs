namespace __GAME__.Core.Localization;

/// <summary>
/// Every localization key referenced from code. Keys are gettext <c>msgid</c>s; the English text
/// lives in <c>localization/en.po</c>, which is the source of truth for which keys exist (ADR 0005).
/// </summary>
/// <remarks>
/// <para>
/// Format: lowercase, dot-separated, snake_case segments (<c>area.context.name</c>). Keys are stable:
/// renaming one means updating every PO file in the same change.
/// </para>
/// <para>
/// Tests verify that every constant here exists in <c>en.po</c> and that every <c>en.po</c> key is
/// referenced here, by content (<see cref="L10nKeyBuilder"/>), or by a scene. Scene labels hold their key
/// directly, so a key used only by a scene is not listed here.
/// </para>
/// </remarks>
public static class L10nKeys
{
    /// <summary>Title screen build label. Placeholder <c>{0}</c>: the build version string.</summary>
    public const string UiTitleBuild = "ui.title.build";
}
