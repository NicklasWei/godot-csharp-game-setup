using System.Text.RegularExpressions;

namespace __GAME__.Core.Tests.TestSupport;

/// <summary>
/// Reads the parts of <c>project.godot</c> that tests validate without an engine: the input map and the registered
/// translation catalogs.
/// </summary>
internal static partial class GodotProjectFile
{
    /// <summary>
    /// Reads every input action and the event types bound to it.
    /// </summary>
    /// <returns>Event class names (e.g. <c>InputEventKey</c>) keyed by action name.</returns>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ReadInputMap()
    {
        var actions = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        string? currentAction = null;
        var currentEvents = new List<string>();
        bool inInputSection = false;

        foreach (string line in File.ReadLines(RepoPaths.ProjectFile))
        {
            if (line.StartsWith('['))
            {
                inInputSection = line == "[input]";
                continue;
            }

            if (!inInputSection)
            {
                continue;
            }

            Match start = ActionStartPattern().Match(line);
            if (start.Success)
            {
                currentAction = start.Groups["name"].Value;
                currentEvents = [];
            }

            foreach (Match match in EventTypePattern().Matches(line))
            {
                currentEvents.Add(match.Groups["type"].Value);
            }

            if (line == "}" && currentAction is not null)
            {
                actions.Add(currentAction, currentEvents);
                currentAction = null;
            }
        }

        return actions;
    }

    /// <summary>
    /// Reads the <c>internationalization/locale/translations</c> setting.
    /// </summary>
    /// <returns>The registered <c>res://</c> paths, in order.</returns>
    public static IReadOnlyList<string> ReadTranslationPaths()
    {
        Match match = TranslationsPattern().Match(File.ReadAllText(RepoPaths.ProjectFile));
        return match.Success
            ? [.. QuotedPattern().Matches(match.Groups["list"].Value).Select(m => m.Groups["value"].Value)]
            : [];
    }

    /// <summary>Matches the first line of an action definition, e.g. <c>pause={</c>.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^(?<name>[a-z0-9_]+)=\{$")]
    private static partial Regex ActionStartPattern();

    /// <summary>Matches a serialized input event, capturing its class name.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"Object\((?<type>InputEvent\w+),")]
    private static partial Regex EventTypePattern();

    /// <summary>Matches the translations setting, capturing the array contents.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^locale/translations=PackedStringArray\((?<list>.*)\)$", RegexOptions.Multiline)]
    private static partial Regex TranslationsPattern();

    /// <summary>Matches a double-quoted string.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex("\"(?<value>[^\"]*)\"")]
    private static partial Regex QuotedPattern();
}
