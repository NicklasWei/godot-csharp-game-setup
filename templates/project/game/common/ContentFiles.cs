using __GAME__.Core.Content;
using __GAME__.Core.Diagnostics;
using Godot;
using FileAccess = Godot.FileAccess;

namespace __GAME__.Game.Common;

/// <summary>
/// Reads every file under <c>res://content/</c> and builds the <see cref="ContentDatabase"/> with Core's
/// <see cref="ContentLoader"/>. Uses Godot's file API, which works both in the editor and in exported builds, where
/// <c>System.IO</c> cannot see packed files.
/// </summary>
/// <remarks>
/// Load-time only (boot); never call it per frame. The export presets keep <c>content/*</c> in the pack and leave
/// <c>content/schemas/</c> out (ADR 0011).
/// </remarks>
public static class ContentFiles
{
    /// <summary>The content root.</summary>
    public const string Root = "res://content";

    /// <summary>The log category for content problems.</summary>
    private const string LogCategory = "Content";

    /// <summary>
    /// Reads, validates, and indexes all content, logging every problem as an error.
    /// </summary>
    /// <param name="log">Where problems and the summary go.</param>
    /// <returns>The valid definitions; broken files are left out (and logged).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="log"/> is <see langword="null"/>.</exception>
    public static ContentDatabase LoadDatabase(ILog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        var files = new List<ContentFile>();
        var readErrors = new List<ContentError>();
        if (DirAccess.DirExistsAbsolute(Root))
        {
            Collect(Root, string.Empty, files, readErrors);
        }

        ContentLoadResult result = ContentLoader.Load(files);
        foreach (ContentError error in readErrors.Concat(result.Errors))
        {
            log.Error(LogCategory, error.ToString());
        }

        log.Info(
            LogCategory,
            $"Loaded {result.Database.Count} definitions from {files.Count} files "
                + $"({readErrors.Count + result.Errors.Count} problems)."
        );
        return result.Database;
    }

    /// <summary>
    /// Reads every file in a directory and its subdirectories, skipping the generated schemas.
    /// </summary>
    /// <param name="directory">The <c>res://</c> directory.</param>
    /// <param name="relative">The directory's content-relative prefix, ending in <c>/</c> (empty at the root).</param>
    /// <param name="files">Receives the files.</param>
    /// <param name="errors">Receives files that could not be read.</param>
    private static void Collect(string directory, string relative, List<ContentFile> files, List<ContentError> errors)
    {
        foreach (string name in DirAccess.GetFilesAt(directory))
        {
            string path = relative + name;
            using FileAccess? file = FileAccess.Open($"{directory}/{name}", FileAccess.ModeFlags.Read);
            if (file is null)
            {
                errors.Add(new ContentError(path, $"Cannot open the file: {FileAccess.GetOpenError()}."));
                continue;
            }

            files.Add(new ContentFile(path, file.GetAsText()));
        }

        foreach (string name in DirAccess.GetDirectoriesAt(directory))
        {
            if (relative.Length == 0 && name == ContentLoader.SchemaFolder)
            {
                continue;
            }

            Collect($"{directory}/{name}", $"{relative}{name}/", files, errors);
        }
    }
}
