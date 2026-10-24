using Godot;

namespace __GAME__.RuntimeTests.Runner;

/// <summary>
/// The scenes the runtime tests load, each loaded once and held until the run ends, as <c>Main</c> holds the game's
/// scenes for a session.
/// </summary>
/// <remarks>
/// <para>
/// A scene loaded from C# and then dropped is held by nothing but its C# wrapper, while Godot's resource cache still
/// hands it out by path. The wrapper's finalizer frees it at a time the GC picks, on the finalizer thread, and so do
/// the finalizers of the wrappers views hold for its materials and sub-scenes. If a load references one of them again
/// after the GC collected its wrapper but before the finalizer ran, Godot leaves the object's instance binding broken
/// and logs <c>Handle is not initialized</c> (godot#112067, ADR 0006). Holding every scene for the run means that never
/// happens: the scenes, and the resources they reference, outlive every test.
/// </para>
/// <para>Main thread only, as are the tests.</para>
/// </remarks>
internal sealed class LoadedScenes
{
    /// <summary>The scenes loaded so far, by <c>res://</c> path.</summary>
    private readonly Dictionary<string, PackedScene> _scenes = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets a scene, loading it the first time.
    /// </summary>
    /// <param name="path">The scene's <c>res://</c> path.</param>
    /// <returns>The scene, or <see langword="null"/> when it didn't load.</returns>
    public PackedScene? Get(string path)
    {
        if (!_scenes.TryGetValue(path, out PackedScene? scene))
        {
            scene = ResourceLoader.Load<PackedScene>(path);
            if (scene is not null)
            {
                _scenes.Add(path, scene);
            }
        }

        return scene;
    }

    /// <summary>
    /// Releases every scene at the end of the run, dropping each C# reference now, on the main thread, rather than
    /// leaving it to a finalizer.
    /// </summary>
    public void Release()
    {
        foreach (PackedScene scene in _scenes.Values)
        {
            scene.Dispose();
        }

        _scenes.Clear();
    }
}
