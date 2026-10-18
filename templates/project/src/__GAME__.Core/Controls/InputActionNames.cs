namespace __GAME__.Core.Controls;

/// <summary>
/// The names of every input action in the project's input map (<c>project.godot</c>, <c>[input]</c> section).
/// Controls are described in the GDD.
/// </summary>
/// <remarks>
/// <para>
/// These are plain strings so Core and the file-based tests can use them without Godot. The Godot
/// layer wraps them as cached <c>StringName</c>s in <c>InputActions</c>.
/// </para>
/// <para>
/// Tests verify that this list and the input map match exactly, and that every action has both a
/// keyboard/mouse and a controller binding. The wiki's controls page lists them with these summaries.
/// </para>
/// </remarks>
public static class InputActionNames
{
    /// <summary>Open or close the pause menu.</summary>
    public const string Pause = "pause";

    /// <summary>Gets every action name, in the order the wiki lists them.</summary>
    public static IReadOnlyList<string> All { get; } = [Pause];
}
