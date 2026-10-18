using System.Globalization;
using __GAME__.Core.Content;
using __GAME__.Core.Localization;

namespace __GAME__.Core.Tests.TestSupport;

/// <summary>
/// Content kinds that exist only in tests, so the loader and its rules are tested independently of the game's
/// real kinds.
/// </summary>
internal static class TestKinds
{
    /// <summary>Gets the <c>widgets</c> kind: files in <c>widgets/</c> holding <c>widget.*</c> IDs.</summary>
    public static ContentKind<WidgetDocument, WidgetDefinition> Widgets { get; } =
        new("widgets", "widget", TestContentJsonContext.Default.WidgetDocument, ValidateWidget, WidgetKeys);

    /// <summary>Gets every test kind.</summary>
    public static IReadOnlyList<IContentKind> All { get; } = [Widgets];

    /// <summary>
    /// Builds a content file for a widget.
    /// </summary>
    /// <param name="name">The file name without extension.</param>
    /// <param name="size">The size in meters to write.</param>
    /// <returns>The file, at <c>widgets/&lt;name&gt;.json</c>.</returns>
    public static ContentFile WidgetFile(string name, float size) =>
        new(
            $"widgets/{name}.json",
            $$"""{ "id": "widget.{{name}}", "size": {{size.ToString(CultureInfo.InvariantCulture)}} }"""
        );

    /// <summary>
    /// Validates a widget document.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="id">The document's ID.</param>
    /// <param name="problems">Receives each problem.</param>
    /// <returns>The widget, or <see langword="null"/> when invalid.</returns>
    private static WidgetDefinition? ValidateWidget(WidgetDocument document, string id, ICollection<string> problems)
    {
        if (document.Size is not > 0f)
        {
            problems.Add("\"size\" must be a positive number of meters.");
            return null;
        }

        return new WidgetDefinition(id, document.Size.Value);
    }

    /// <summary>
    /// Lists the keys a widget needs.
    /// </summary>
    /// <param name="widget">The widget.</param>
    /// <returns>Its name key.</returns>
    private static IEnumerable<string> WidgetKeys(WidgetDefinition widget) => [L10nKeyBuilder.Name(widget.Id)];
}
