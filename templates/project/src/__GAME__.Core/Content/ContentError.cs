namespace __GAME__.Core.Content;

/// <summary>
/// One problem found while loading content, tied to the file it came from.
/// </summary>
/// <param name="Path">The content-relative path of the file, e.g. <c>items/iron_sword.json</c>.</param>
/// <param name="Message">What is wrong, for the developer reading the log.</param>
public sealed record ContentError(string Path, string Message)
{
    /// <inheritdoc/>
    public override string ToString() => $"{Path}: {Message}";
}
