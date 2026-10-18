namespace __GAME__.Core.Tests.TestSupport;

/// <summary>
/// Locates files in the repository so data tests can validate the real project files
/// (<c>project.godot</c>, PO catalogs, scenes, content).
/// </summary>
internal static class RepoPaths
{
    /// <summary>Gets the repository root: the directory containing <c>project.godot</c>.</summary>
    public static string Root { get; } = FindRoot();

    /// <summary>Gets the path of <c>project.godot</c>.</summary>
    public static string ProjectFile => Path.Combine(Root, "project.godot");

    /// <summary>Gets the localization directory.</summary>
    public static string Localization => Path.Combine(Root, "localization");

    /// <summary>Gets the content definitions directory.</summary>
    public static string Content => Path.Combine(Root, "content");

    /// <summary>Gets the Godot game-layer directory.</summary>
    public static string Game => Path.Combine(Root, "game");

    /// <summary>
    /// Walks up from the test assembly's directory until it finds <c>project.godot</c>.
    /// </summary>
    /// <returns>The repository root.</returns>
    /// <exception cref="InvalidOperationException">No ancestor contains <c>project.godot</c>.</exception>
    private static string FindRoot()
    {
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "project.godot")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("Could not find project.godot above " + AppContext.BaseDirectory);
    }
}
