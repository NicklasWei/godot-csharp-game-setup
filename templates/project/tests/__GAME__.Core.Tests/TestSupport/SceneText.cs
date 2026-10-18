namespace __GAME__.Core.Tests.TestSupport;

/// <summary>
/// A translatable text value found in a scene file.
/// </summary>
/// <param name="ScenePath">The scene path relative to the repository root.</param>
/// <param name="NodeName">The node that holds the text.</param>
/// <param name="Text">The text value; expected to be a localization key.</param>
internal sealed record SceneText(string ScenePath, string NodeName, string Text);
