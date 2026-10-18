using System.Text.RegularExpressions;

namespace __GAME__.Core.Tests.TestSupport;

/// <summary>
/// Finds translatable <c>text</c> properties in the game's <c>.tscn</c> files. A node's text is
/// translatable unless the node sets <c>auto_translate_mode = 2</c> (disabled).
/// </summary>
internal static partial class SceneTextScanner
{
    /// <summary>
    /// Scans every scene under <c>game/</c>.
    /// </summary>
    /// <returns>Each translatable text value with the scene and node it came from.</returns>
    public static IReadOnlyList<SceneText> FindTranslatableTexts()
    {
        var results = new List<SceneText>();
        foreach (string scene in Directory.EnumerateFiles(RepoPaths.Game, "*.tscn", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(RepoPaths.Root, scene).Replace('\\', '/');
            foreach (string block in File.ReadAllText(scene).ReplaceLineEndings("\n").Split("\n["))
            {
                Match node = NodeHeaderPattern().Match(block);
                Match text = TextPattern().Match(block);
                if (
                    node.Success
                    && text.Success
                    && text.Groups["value"].Value.Length > 0
                    && !block.Contains("auto_translate_mode = 2", StringComparison.Ordinal)
                )
                {
                    results.Add(new SceneText(relative, node.Groups["name"].Value, text.Groups["value"].Value));
                }
            }
        }

        return results;
    }

    /// <summary>Matches a node header and captures its name.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex("^\\[?node name=\"(?<name>[^\"]+)\"")]
    private static partial Regex NodeHeaderPattern();

    /// <summary>Matches a single-line <c>text</c> property and captures its value.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex("^text = \"(?<value>[^\"]*)\"$", RegexOptions.Multiline)]
    private static partial Regex TextPattern();
}
