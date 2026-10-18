using System.Diagnostics.CodeAnalysis;

namespace __GAME__.Wiki;

/// <summary>
/// The generator's command line.
/// </summary>
/// <param name="RepoRoot">The repository root: the folder holding <c>project.godot</c>.</param>
/// <param name="OutputDirectory">Where the pages go; the folder is replaced on every run.</param>
/// <param name="Commit">The commit named in page headers.</param>
/// <param name="SourceUrl">
/// The prefix that turns a repository path into a link (a <c>…/-/blob/&lt;sha&gt;/</c> or <c>…/blob/&lt;sha&gt;/</c>
/// URL), or <see langword="null"/> for plain paths.
/// </param>
/// <param name="Strict">Whether any problem (a content error, a missing string or doc) fails the run.</param>
internal sealed record WikiOptions(string RepoRoot, string OutputDirectory, string Commit, string? SourceUrl, bool Strict)
{
    /// <summary>The usage text.</summary>
    public const string Usage =
        "Usage: __GAME__.Wiki [--repo <root>] [--out <dir>] [--commit <sha>] [--source-url <url>] [--strict]";

    /// <summary>
    /// Parses the command line. The repository defaults to the nearest folder above the working directory that holds
    /// <c>project.godot</c>, and the output to <c>tools/wiki/generated</c> under it.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <param name="options">The options, when they parse.</param>
    /// <param name="error">What is wrong, when they don't.</param>
    /// <returns>Whether the arguments parsed.</returns>
    public static bool TryParse(
        IReadOnlyList<string> args,
        [NotNullWhen(true)] out WikiOptions? options,
        [NotNullWhen(false)] out string? error
    )
    {
        options = null;
        string? repo = null;
        string? output = null;
        string commit = "local";
        string? sourceUrl = null;
        bool strict = false;

        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            if (arg == "--strict")
            {
                strict = true;
                continue;
            }

            if (i + 1 >= args.Count)
            {
                error = "Missing value for " + arg + ".";
                return false;
            }

            string value = args[++i];
            switch (arg)
            {
                case "--repo":
                    repo = value;
                    break;
                case "--out":
                    output = value;
                    break;
                case "--commit":
                    commit = value;
                    break;
                case "--source-url":
                    sourceUrl = value.EndsWith('/') ? value : value + "/";
                    break;
                default:
                    error = "Unknown argument " + arg + ".";
                    return false;
            }
        }

        repo = repo is null ? FindRepoRoot(Directory.GetCurrentDirectory()) : Path.GetFullPath(repo);
        if (repo is null || !File.Exists(Path.Combine(repo, "project.godot")))
        {
            error = "Could not find the repository root (a folder with project.godot); pass --repo.";
            return false;
        }

        output = Path.GetFullPath(output ?? Path.Combine(repo, "tools", "wiki", "generated"));
        options = new WikiOptions(repo, output, commit, sourceUrl, strict);
        error = null;
        return true;
    }

    /// <summary>
    /// Walks up from a folder to the one holding <c>project.godot</c>.
    /// </summary>
    /// <param name="start">The folder to start in.</param>
    /// <returns>The repository root, or <see langword="null"/>.</returns>
    private static string? FindRepoRoot(string start)
    {
        for (DirectoryInfo? dir = new(start); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "project.godot")))
            {
                return dir.FullName;
            }
        }

        return null;
    }
}
