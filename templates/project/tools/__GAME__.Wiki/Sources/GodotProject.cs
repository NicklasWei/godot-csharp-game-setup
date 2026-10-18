using System.Globalization;
using System.Text.RegularExpressions;

namespace __GAME__.Wiki.Sources;

/// <summary>
/// Reads the input map from <c>project.godot</c> as readable bindings, for the controls page.
/// </summary>
internal static partial class GodotProject
{
    /// <summary>The modifier flags of a key or mouse event, with the prefix each adds to a binding's label.</summary>
    private static readonly (string Field, string Label)[] ModifierLabels =
    [
        ("ctrl_pressed", "Ctrl+"),
        ("shift_pressed", "Shift+"),
        ("alt_pressed", "Alt+"),
        ("meta_pressed", "Meta+"),
    ];

    /// <summary>
    /// Reads every input action and its bindings.
    /// </summary>
    /// <param name="projectFile">The path of <c>project.godot</c>.</param>
    /// <returns>The bindings, keyed by action name, in file order.</returns>
    public static IReadOnlyDictionary<string, IReadOnlyList<InputBinding>> ReadInputMap(string projectFile)
    {
        string text = File.ReadAllText(projectFile).ReplaceLineEndings("\n");
        Match section = InputSectionPattern().Match(text);
        var actions = new Dictionary<string, IReadOnlyList<InputBinding>>(StringComparer.Ordinal);
        if (!section.Success)
        {
            return actions;
        }

        foreach (Match action in ActionPattern().Matches(section.Groups["body"].Value))
        {
            var bindings = new List<InputBinding>();
            foreach (Match input in EventPattern().Matches(action.Groups["events"].Value))
            {
                Dictionary<string, string> fields = PropertyPattern()
                    .Matches(input.Groups["fields"].Value)
                    .ToDictionary(m => m.Groups["key"].Value, m => m.Groups["value"].Value, StringComparer.Ordinal);
                if (Describe(input.Groups["type"].Value, fields) is { } binding)
                {
                    bindings.Add(binding);
                }
            }

            actions[action.Groups["name"].Value] = bindings;
        }

        return actions;
    }

    /// <summary>
    /// Turns one serialized input event into a readable binding.
    /// </summary>
    /// <param name="type">The event class, e.g. <c>InputEventKey</c>.</param>
    /// <param name="fields">The event's serialized properties.</param>
    /// <returns>The binding, or <see langword="null"/> for an event type the page doesn't describe.</returns>
    private static InputBinding? Describe(string type, IReadOnlyDictionary<string, string> fields)
    {
        switch (type)
        {
            case "InputEventKey":
                long code = Number(fields, "physical_keycode") is var physical and not 0
                    ? physical
                    : Number(fields, "keycode");
                return new InputBinding(false, Modifiers(fields) + InputNames.Key(code));
            case "InputEventMouseButton":
                return new InputBinding(false, Modifiers(fields) + InputNames.MouseButton(Number(fields, "button_index")));
            case "InputEventJoypadButton":
                return new InputBinding(true, InputNames.JoypadButton(Number(fields, "button_index")));
            case "InputEventJoypadMotion":
                double value = double.TryParse(
                    fields.GetValueOrDefault("axis_value"),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double parsed
                )
                    ? parsed
                    : 0;
                return new InputBinding(true, InputNames.JoypadAxis(Number(fields, "axis"), value));
            default:
                return null;
        }
    }

    /// <summary>
    /// Reads the modifier prefix of a key or mouse event, e.g. <c>Ctrl+</c>.
    /// </summary>
    /// <param name="fields">The event's properties.</param>
    /// <returns>The prefix, or empty.</returns>
    private static string Modifiers(IReadOnlyDictionary<string, string> fields) =>
        string.Concat(
            ModifierLabels
                .Where(modifier => fields.GetValueOrDefault(modifier.Field) == "true")
                .Select(modifier => modifier.Label)
        );

    /// <summary>
    /// Reads an integer property.
    /// </summary>
    /// <param name="fields">The event's properties.</param>
    /// <param name="key">The property name.</param>
    /// <returns>The value, or 0 when absent or not a number.</returns>
    private static long Number(IReadOnlyDictionary<string, string> fields, string key) =>
        long.TryParse(fields.GetValueOrDefault(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out long n)
            ? n
            : 0;

    /// <summary>Matches the <c>[input]</c> section, capturing its body.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^\[input\]\n(?<body>.*?)(?=^\[|\z)", RegexOptions.Singleline | RegexOptions.Multiline)]
    private static partial Regex InputSectionPattern();

    /// <summary>Matches one action, capturing its name and its events array.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(
        @"^(?<name>[a-z0-9_]+)=\{\n.*?""events"": \[(?<events>.*?)\]\n\}$",
        RegexOptions.Singleline | RegexOptions.Multiline
    )]
    private static partial Regex ActionPattern();

    /// <summary>Matches one serialized event, capturing its class and its property lines.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"Object\((?<type>InputEvent\w+),(?<fields>.*?)\n\)", RegexOptions.Singleline)]
    private static partial Regex EventPattern();

    /// <summary>Matches one <c>"key": value</c> property line.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"""(?<key>\w+)"": (?<value>[^,\n]+)")]
    private static partial Regex PropertyPattern();
}
