using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace __GAME__.RuntimeTests.Runner;

/// <summary>
/// What a Godot-runtime test gets: a fresh container node in the live scene tree, freed after the test, the run's
/// scenes, and helpers to wait for frames.
/// </summary>
public sealed class GodotTestContext
{
    /// <summary>How many physics ticks <see cref="WaitUntil"/> waits between checks.</summary>
    private const int WaitStepTicks = 10;

    /// <summary>The least real time <see cref="WaitUntil"/> waits before giving up.</summary>
    private static readonly TimeSpan MinimumWait = TimeSpan.FromSeconds(10);

    /// <summary>The scenes loaded for the run.</summary>
    private readonly LoadedScenes _scenes;

    /// <summary>
    /// Initializes a new instance of the <see cref="GodotTestContext"/> class.
    /// </summary>
    /// <param name="root">The test's container node; already in the tree.</param>
    /// <param name="scenes">The scenes loaded for the run.</param>
    internal GodotTestContext(Node root, LoadedScenes scenes)
    {
        Root = root;
        _scenes = scenes;
    }

    /// <summary>Gets the test's container node. Add scenes under it; the runner frees it after the test.</summary>
    public Node Root { get; }

    /// <summary>Gets the running scene tree.</summary>
    public SceneTree Tree => Root.GetTree();

    /// <summary>
    /// Gets a scene, loaded once for the run and held until it ends. Tests load scenes through this, never with
    /// <c>ResourceLoader</c>: a scene they dropped would be freed later by a finalizer, which can break a load in
    /// another test (godot#112067, ADR 0006).
    /// </summary>
    /// <param name="path">The scene's <c>res://</c> path.</param>
    /// <returns>The scene.</returns>
    /// <exception cref="GodotTestFailedException">The scene is missing.</exception>
    public PackedScene Scene(string path) =>
        _scenes.Get(path) ?? throw new GodotTestFailedException($"{path} did not load.");

    /// <summary>
    /// Gets a scene as <see cref="Scene"/> does, for a test that checks whether it loads.
    /// </summary>
    /// <param name="path">The scene's <c>res://</c> path.</param>
    /// <param name="scene">The scene, when it loaded.</param>
    /// <returns>Whether it loaded.</returns>
    public bool TryScene(string path, [NotNullWhen(true)] out PackedScene? scene)
    {
        scene = _scenes.Get(path);
        return scene is not null;
    }

    /// <summary>
    /// Instantiates one of the run's scenes (<see cref="Scene"/>) without adding it to the tree.
    /// </summary>
    /// <typeparam name="T">The scene root's type.</typeparam>
    /// <param name="path">The scene's <c>res://</c> path.</param>
    /// <returns>The instance.</returns>
    /// <exception cref="GodotTestFailedException">The scene is missing or its root isn't a <typeparamref name="T"/>.</exception>
    public T Instantiate<T>(string path)
        where T : Node =>
        Scene(path).Instantiate() as T
        ?? throw new GodotTestFailedException($"{path}'s root is not a {typeof(T).Name}.");

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
    /// Waits, a few physics ticks at a time, until a condition holds, giving up only once both the ticks and
    /// <see cref="MinimumWait"/> of real time have passed.
    /// </summary>
    /// <remarks>
    /// The ticks bound what the game does in game time; the real-time minimum leaves room for work on a worker, such
    /// as a background load, which takes real time whatever the frame rate. At a fixed fps the ticks pass in a moment
    /// (ADR 0006); at real-time pacing they outlast the minimum, which then changes nothing.
    /// </remarks>
    /// <param name="condition">The condition.</param>
    /// <param name="maxTicks">The fewest physics ticks to wait before giving up.</param>
    /// <returns><see langword="true"/> when the condition held.</returns>
    public async Task<bool> WaitUntil(Func<bool> condition, int maxTicks)
    {
        ArgumentNullException.ThrowIfNull(condition);
        long start = Stopwatch.GetTimestamp();
        for (int ticks = 0; !condition(); ticks += WaitStepTicks)
        {
            if (ticks >= maxTicks && Stopwatch.GetElapsedTime(start) >= MinimumWait)
            {
                return false;
            }

            await PhysicsFrames(WaitStepTicks);
        }

        return true;
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
