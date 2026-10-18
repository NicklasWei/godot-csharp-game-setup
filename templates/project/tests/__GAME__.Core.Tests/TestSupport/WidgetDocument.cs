using __GAME__.Core.Content;

namespace __GAME__.Core.Tests.TestSupport;

/// <summary>
/// The JSON shape of the test-only <c>widgets</c> content kind (<see cref="TestKinds.Widgets"/>), which exercises
/// the content pipeline without depending on the game's real kinds.
/// </summary>
internal sealed class WidgetDocument : ContentDocument
{
    /// <summary>Gets or sets the widget's size in meters; required and positive.</summary>
    public float? Size { get; set; }
}
