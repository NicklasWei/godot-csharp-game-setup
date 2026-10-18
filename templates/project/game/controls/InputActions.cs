using __GAME__.Core.Controls;
using Godot;

namespace __GAME__.Game.Controls;

/// <summary>
/// The input action names as cached <see cref="StringName"/>s, so polling input never converts a string (which
/// allocates on every call). The names come from Core's <see cref="InputActionNames"/>.
/// </summary>
/// <remarks>
/// The folder is <c>controls</c>, not <c>input</c>: a <c>__GAME__.Game.Input</c> namespace would shadow Godot's
/// <c>Input</c> class everywhere below it.
/// </remarks>
public static class InputActions
{
    /// <summary>Open or close the pause menu.</summary>
    public static readonly StringName Pause = InputActionNames.Pause;
}
