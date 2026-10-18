namespace __GAME__.Core.Content;

/// <summary>
/// What <see cref="ContentLoader.Load(IEnumerable{ContentFile})"/> produced: the definitions that validated, and
/// every problem found.
/// </summary>
/// <param name="Database">The valid definitions; files with errors are left out.</param>
/// <param name="Errors">Every problem, in file path order; empty when all content is valid.</param>
public sealed record ContentLoadResult(ContentDatabase Database, IReadOnlyList<ContentError> Errors)
{
    /// <summary>Gets a value indicating whether every file loaded without problems.</summary>
    public bool IsValid => Errors.Count == 0;
}
