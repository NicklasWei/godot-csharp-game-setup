using System.Reflection;
using __GAME__.Core.Controls;
using __GAME__.Wiki.Markdown;
using __GAME__.Wiki.Sources;

namespace __GAME__.Wiki.Pages;

/// <summary>
/// The controls page: every input action from <see cref="InputActionNames"/>, its description, and its default
/// keyboard/mouse and controller bindings from <c>project.godot</c>.
/// </summary>
internal static class ControlsPage
{
    private const string Page = "controls.md";

    /// <summary>
    /// Writes the controls page.
    /// </summary>
    /// <param name="context">The wiki context.</param>
    /// <returns>The page.</returns>
    public static IEnumerable<GeneratedPage> Write(WikiContext context)
    {
        MarkdownWriter writer = context.StartPage(
            "Controls",
            "project.godot",
            WikiContext.CoreFolder(typeof(InputActionNames)) + "InputActionNames.cs"
        );
        writer.Paragraph(
            "Default bindings. Every action has both a keyboard/mouse and a controller binding (the input map tests "
                + "enforce it)."
        );

        IReadOnlyDictionary<string, IReadOnlyList<InputBinding>> map = GodotProject.ReadInputMap(
            Path.Combine(context.Options.RepoRoot, "project.godot")
        );
        Dictionary<string, FieldInfo> constants = typeof(InputActionNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.GetRawConstantValue() is string)
            .ToDictionary(field => (string)(field.GetRawConstantValue() ?? string.Empty), StringComparer.Ordinal);

        writer.Table(
            ["Action", "Description", "Keyboard and mouse", "Controller"],
            InputActionNames.All.Select(action =>
            {
                IReadOnlyList<InputBinding> bindings = map.GetValueOrDefault(action) ?? [];
                if (!map.ContainsKey(action))
                {
                    context.Report("Input action missing from project.godot: " + action);
                }

                string description = constants.TryGetValue(action, out FieldInfo? field)
                    ? context.Describe(field) ?? ValueFormatter.None
                    : ValueFormatter.None;
                return MarkdownWriter.Row(
                    MarkdownWriter.Code(action),
                    description,
                    Join(bindings.Where(binding => !binding.IsController)),
                    Join(bindings.Where(binding => binding.IsController))
                );
            })
        );

        foreach (string action in map.Keys.Where(action => !InputActionNames.All.Contains(action)))
        {
            context.Report("Input action in project.godot but not in InputActionNames: " + action);
        }

        yield return new GeneratedPage(Page, "Controls", TuningPage.Group, writer.ToString());
    }

    /// <summary>
    /// Joins binding labels for one cell.
    /// </summary>
    /// <param name="bindings">The bindings.</param>
    /// <returns>The labels, or <see cref="ValueFormatter.None"/>.</returns>
    private static string Join(IEnumerable<InputBinding> bindings)
    {
        string[] labels = [.. bindings.Select(binding => binding.Label)];
        return labels.Length == 0 ? ValueFormatter.None : string.Join(", ", labels);
    }
}
