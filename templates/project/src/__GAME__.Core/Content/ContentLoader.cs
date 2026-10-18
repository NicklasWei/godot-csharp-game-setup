namespace __GAME__.Core.Content;

/// <summary>
/// Loads every content file into a <see cref="ContentDatabase"/> and collects every problem, so one boot reports all
/// broken content at once (Coding Standards §3.7, ADR 0007).
/// </summary>
/// <remarks>
/// <para><b>Rules</b> on top of each kind's own validation:</para>
/// <list type="bullet">
/// <item>Files live one level down, in a registered kind's folder. <c>schemas/</c> is skipped.</item>
/// <item>Only <c>.json</c> files belong in content.</item>
/// <item>The ID is the kind's prefix plus the file name: <c>items/iron_sword.json</c> holds <c>item.iron_sword</c>.
/// Names are snake_case (<c>[a-z][a-z0-9_]*</c>). This keeps IDs unique and easy to find.</item>
/// <item>Unknown JSON properties are errors (<c>ContentJsonContext</c>), so typos never pass silently.</item>
/// <item>A broken file is reported and left out; the rest still load.</item>
/// </list>
/// <para>Load-time only: it allocates freely.</para>
/// </remarks>
public static class ContentLoader
{
    /// <summary>The folder of generated JSON Schemas, which is not content.</summary>
    public const string SchemaFolder = "schemas";

    /// <summary>The extension of content files.</summary>
    private const string Extension = ".json";

    /// <summary>
    /// Validates and indexes content files against the registered kinds (<see cref="ContentKinds.All"/>).
    /// </summary>
    /// <param name="files">Every file under the content root, in any order.</param>
    /// <returns>The valid definitions plus every problem, in path order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> is <see langword="null"/>.</exception>
    public static ContentLoadResult Load(IEnumerable<ContentFile> files) => Load(files, ContentKinds.All);

    /// <summary>
    /// Validates and indexes content files against the given kinds.
    /// </summary>
    /// <param name="files">Every file under the content root, in any order.</param>
    /// <param name="kinds">The kinds to load; their folders must be distinct.</param>
    /// <returns>The valid definitions plus every problem, in path order.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Two kinds share a folder.</exception>
    public static ContentLoadResult Load(IEnumerable<ContentFile> files, IReadOnlyList<IContentKind> kinds)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(kinds);
        var byFolder = new Dictionary<string, IContentKind>(StringComparer.Ordinal);
        foreach (IContentKind kind in kinds)
        {
            if (!byFolder.TryAdd(kind.Folder, kind))
            {
                throw new ArgumentException($"Two content kinds use the folder \"{kind.Folder}\".", nameof(kinds));
            }
        }

        var errors = new List<ContentError>();
        var definitions = new List<IContentDefinition>();
        var owners = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (ContentFile file in files.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            if (!TrySplitPath(file.Path, errors, out string folder, out string name))
            {
                continue;
            }

            if (!byFolder.TryGetValue(folder, out IContentKind? kind))
            {
                errors.Add(new ContentError(file.Path, UnknownFolderMessage(folder, byFolder.Keys)));
                continue;
            }

            if (!kind.TryParse(file.Text, out IContentDefinition? definition, out IReadOnlyList<string> problems))
            {
                errors.AddRange(problems.Select(problem => new ContentError(file.Path, problem)));
                continue;
            }

            string expectedId = kind.IdPrefix + "." + name;
            if (definition.Id != expectedId)
            {
                errors.Add(
                    new ContentError(file.Path, $"The ID \"{definition.Id}\" must match the file name: \"{expectedId}\".")
                );
            }
            else if (!owners.TryAdd(definition.Id, file.Path))
            {
                errors.Add(
                    new ContentError(file.Path, $"The ID \"{definition.Id}\" is already defined in {owners[definition.Id]}.")
                );
            }
            else
            {
                definitions.Add(definition);
            }
        }

        return new ContentLoadResult(new ContentDatabase(definitions), errors);
    }

    /// <summary>
    /// Checks a name against <c>[a-z][a-z0-9_]*</c>.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns><see langword="true"/> when it is snake_case.</returns>
    public static bool IsSnakeCase(string? name)
    {
        if (string.IsNullOrEmpty(name) || !char.IsAsciiLetterLower(name[0]))
        {
            return false;
        }

        foreach (char c in name)
        {
            if (!char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '_')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Splits a content path into its kind folder and file name, rejecting misplaced files.
    /// </summary>
    /// <param name="path">The content-relative path.</param>
    /// <param name="errors">Receives a problem for a misplaced file.</param>
    /// <param name="folder">The kind folder.</param>
    /// <param name="name">The file name without extension.</param>
    /// <returns><see langword="true"/> for a content file to parse; <see langword="false"/> for schemas and bad paths.</returns>
    private static bool TrySplitPath(string path, List<ContentError> errors, out string folder, out string name)
    {
        string[] parts = path.Split('/');
        folder = parts[0];
        name = string.Empty;
        if (folder == SchemaFolder)
        {
            return false;
        }

        if (parts.Length != 2)
        {
            errors.Add(new ContentError(path, "Content files belong directly inside a kind folder, e.g. items/."));
            return false;
        }

        if (!parts[1].EndsWith(Extension, StringComparison.Ordinal))
        {
            errors.Add(new ContentError(path, "Only .json files belong in content."));
            return false;
        }

        name = parts[1][..^Extension.Length];
        if (!IsSnakeCase(name))
        {
            errors.Add(new ContentError(path, "File names are snake_case: a lowercase letter, then a-z, 0-9, or _."));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Describes an unregistered folder.
    /// </summary>
    /// <param name="folder">The folder found.</param>
    /// <param name="known">The registered folders.</param>
    /// <returns>The message.</returns>
    private static string UnknownFolderMessage(string folder, IEnumerable<string> known)
    {
        string[] folders = [.. known.Order(StringComparer.Ordinal)];
        return folders.Length == 0
            ? $"Unknown content folder \"{folder}\": no content kinds are registered (ContentKinds.All)."
            : $"Unknown content folder \"{folder}\"; expected one of {string.Join(", ", folders)}.";
    }
}
