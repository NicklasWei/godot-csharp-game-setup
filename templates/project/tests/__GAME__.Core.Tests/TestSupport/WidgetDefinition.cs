using __GAME__.Core.Content;

namespace __GAME__.Core.Tests.TestSupport;

/// <summary>
/// A validated widget from the test-only <c>widgets</c> content kind.
/// </summary>
/// <param name="Id">The permanent ID, e.g. <c>widget.small</c>.</param>
/// <param name="Size">The size in meters; positive.</param>
internal sealed record WidgetDefinition(string Id, float Size) : IContentDefinition;
