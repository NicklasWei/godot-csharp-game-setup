using Godot;

namespace __GAME__.RuntimeTests.Runner;

/// <summary>
/// What a Godot-runtime test gets: a fresh container node in the live scene tree, freed after the test, and helpers
/// to wait for frames.
/// </summary>
public sealed class GodotTestContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GodotTestContext"/> class.
    /// </summary>
    /// <param name="root">The test's container node; already in the tree.</param>
    internal GodotTestContext(Node root)
    {
        Root = root;
    }

    /// <summary>Gets the test's container node. Add scenes under it; the runner frees it after the test.</summary>
    public Node Root { get; }

    /// <summary>Gets the running scene tree.</summary>
    public SceneTree Tree => Root.GetTree();

    /// <summary>
    /// Loads and instantiates a scene without adding it to the tree.
    /// </summary>
    /// <typeparam name="T">The scene root's type.</typeparam>
    /// <param name="path">The scene's <c>res://</c> path.</param>
    /// <returns>The instance.</returns>
    /// <exception cref="GodotTestFailedException">The scene is missing or its root isn't a <typeparamref name="T"/>.</exception>
    public static T Instantiate<T>(string path)
        where T : Node
    {
        PackedScene scene =
            ResourceLoader.Load<PackedScene>(path) ?? throw new GodotTestFailedException($"{path} did not load.");
        return scene.Instantiate() as T
            ?? throw new GodotTestFailedException($"{path}'s root is not a {typeof(T).Name}.");
    }

    /// <summary>
    /// Waits for physics ticks (gameplay steps).
    /// </summary>
    /// <param name="count">How many ticks to wait.</param>
    /// <returns>A task that completes after the ticks.</returns>
    public async Task PhysicsFrames(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await Root.ToSignal(Tree, SceneTree.SignalName.PhysicsFrame);
        }
    }

    /// <summary>
    /// Waits for rendered frames.
    /// </summary>
    /// <param name="count">How many frames to wait.</param>
    /// <returns>A task that completes after the frames.</returns>
    public async Task ProcessFrames(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await Root.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
