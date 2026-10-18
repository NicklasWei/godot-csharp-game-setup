namespace __GAME__.Wiki.Sources;

/// <summary>
/// One event bound to an input action in <c>project.godot</c>, as a readable label.
/// </summary>
/// <param name="IsController">Whether it is a controller binding (otherwise keyboard or mouse).</param>
/// <param name="Label">The readable binding, e.g. <c>Escape</c>, <c>Left mouse button</c>, or <c>Start</c>.</param>
internal sealed record InputBinding(bool IsController, string Label);
