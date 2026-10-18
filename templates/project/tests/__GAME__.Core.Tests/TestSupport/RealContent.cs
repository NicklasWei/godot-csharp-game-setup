using __GAME__.Core.Content;

namespace __GAME__.Core.Tests.TestSupport;

/// <summary>
/// Loads the repository's real <c>content/</c> folder from disk, the same way the game reads it through Godot.
/// </summary>
internal static class RealContent
{
    /// <summary>
    /// Reads every content file except the schemas, as content-relative forward-slash paths.
    /// </summary>
    /// <returns>The files.</returns>
    public static IReadOnlyList<ContentFile> Files() =>
        [
            .. Directory
                .EnumerateFiles(RepoPaths.Content, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(RepoPaths.Content, path).Replace('\\', '/'))
                .Where(path => !path.StartsWith(ContentLoader.SchemaFolder + "/", StringComparison.Ordinal))
                .Select(path => new ContentFile(path, File.ReadAllText(Path.Combine(RepoPaths.Content, path)))),
        ];

    /// <summary>
    /// Loads the real content against the game's registered kinds.
    /// </summary>
    /// <returns>The load result.</returns>
    public static ContentLoadResult Load() => ContentLoader.Load(Files());
}
