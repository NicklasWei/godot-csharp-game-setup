using __GAME__.RuntimeTests.Runner;
using Godot;

namespace __GAME__.RuntimeTests.Scenes;

/// <summary>
/// Every scene in the game and dev folders loads (its resources and scripts resolve), instantiates, and frees
/// without leaving orphan nodes behind (such as a child a script makes before it is ever added).
/// </summary>
public sealed class SceneLoadTests
{
    /// <summary>The folders whose scenes must load.</summary>
    private static readonly string[] Roots = ["res://game", "res://dev"];

    [GodotTest]
    public static async Task EveryScene_LoadsInstantiatesAndFreesCleanly(GodotTestContext context)
    {
        // Arrange
        double orphansBefore = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);
        var scenes = new List<string>();
        foreach (string root in Roots)
        {
            CollectScenes(root, scenes);
        }

        var failures = new List<string>();

        // Act
        foreach (string path in scenes)
        {
            Node? instance = context.TryScene(path, out PackedScene? scene) ? scene.Instantiate() : null;
            if (instance is null)
            {
                failures.Add(path);
                continue;
            }

            instance.QueueFree();
        }

        await context.ProcessFrames(2);
        double orphansAfter = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);

        // Assert
        GodotAssert.True(scenes.Count >= 2, $"Expected at least the main and title scenes, found {scenes.Count}.");
        GodotAssert.True(failures.Count == 0, "Scenes that did not load: " + string.Join(", ", failures));
        GodotAssert.True(
            orphansAfter <= orphansBefore,
            $"Freeing the scenes left {orphansAfter - orphansBefore} orphan nodes behind."
        );
    }

    /// <summary>
    /// Collects every <c>.tscn</c> under a folder, recursively; a missing folder has none.
    /// </summary>
    /// <param name="folder">A <c>res://</c> folder.</param>
    /// <param name="scenes">Receives the scene paths.</param>
    private static void CollectScenes(string folder, List<string> scenes)
    {
        if (!DirAccess.DirExistsAbsolute(folder))
        {
            return;
        }

        foreach (string file in DirAccess.GetFilesAt(folder))
        {
            if (file.EndsWith(".tscn", StringComparison.Ordinal))
            {
                scenes.Add($"{folder}/{file}");
            }
        }

        foreach (string child in DirAccess.GetDirectoriesAt(folder))
        {
            CollectScenes($"{folder}/{child}", scenes);
        }
    }
}
