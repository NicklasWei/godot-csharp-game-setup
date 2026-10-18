namespace __GAME__.Wiki;

/// <summary>
/// One generated Markdown page.
/// </summary>
/// <param name="Path">The path under the reference section, with forward slashes, e.g. <c>tuning.md</c>.</param>
/// <param name="Title">The page's navigation title.</param>
/// <param name="Group">The navigation group it is listed under, or <see langword="null"/> for the top level.</param>
/// <param name="Text">The page's Markdown, with LF line endings.</param>
internal sealed record GeneratedPage(string Path, string Title, string? Group, string Text);
