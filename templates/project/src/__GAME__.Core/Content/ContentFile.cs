namespace __GAME__.Core.Content;

/// <summary>
/// One content file handed to <see cref="ContentLoader"/>: its path and text. The game layer reads files
/// from <c>res://content/</c>, and tests and tools read the same folder from disk.
/// </summary>
/// <param name="Path">The path relative to the content root, with forward slashes, e.g. <c>items/iron_sword.json</c>.</param>
/// <param name="Text">The whole file as text.</param>
public sealed record ContentFile(string Path, string Text);
