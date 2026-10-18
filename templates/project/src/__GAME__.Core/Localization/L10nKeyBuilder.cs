namespace __GAME__.Core.Localization;

/// <summary>
/// Builds the localization keys that derive from content IDs, so every caller (game, tests, wiki) spells them the
/// same way: a definition <c>item.iron_sword</c> has its name at <c>item.iron_sword.name</c>.
/// </summary>
public static class L10nKeyBuilder
{
    /// <summary>
    /// Gets the key of a content definition's player-facing name.
    /// </summary>
    /// <param name="contentId">The permanent content ID, e.g. <c>item.iron_sword</c>.</param>
    /// <returns>The key, e.g. <c>item.iron_sword.name</c>.</returns>
    /// <exception cref="ArgumentException"><paramref name="contentId"/> is null or empty.</exception>
    public static string Name(string contentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentId);
        return contentId + ".name";
    }

    /// <summary>
    /// Gets the key of a content definition's player-facing description.
    /// </summary>
    /// <param name="contentId">The permanent content ID, e.g. <c>item.iron_sword</c>.</param>
    /// <returns>The key, e.g. <c>item.iron_sword.desc</c>.</returns>
    /// <exception cref="ArgumentException"><paramref name="contentId"/> is null or empty.</exception>
    public static string Description(string contentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentId);
        return contentId + ".desc";
    }
}
