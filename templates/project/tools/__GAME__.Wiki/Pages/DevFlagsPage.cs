using System.Reflection;
using __GAME__.Core.Development;
using __GAME__.Wiki.Markdown;

namespace __GAME__.Wiki.Pages;

/// <summary>
/// The development switches page: every flag <see cref="DevFlags"/> declares, with its command-line argument and
/// what it does.
/// </summary>
/// <remarks>
/// A flag property <c>X</c> pairs with the constant <c>XArgument</c> (a switch) or <c>XPrefix</c> (an argument that
/// takes a value, <c>--x=N</c>). A flag without one, or a constant without a flag, is reported.
/// </remarks>
internal static class DevFlagsPage
{
    private const string Page = "dev-flags.md";

    /// <summary>
    /// Writes the page.
    /// </summary>
    /// <param name="context">The wiki context.</param>
    /// <returns>The page.</returns>
    public static IEnumerable<GeneratedPage> Write(WikiContext context)
    {
        MarkdownWriter writer = context.StartPage(
            "Dev flags",
            WikiContext.CoreFolder(typeof(DevFlags)) + "DevFlags.cs"
        );
        writer.Paragraph(context.Docs.Summary(typeof(DevFlags)));

        Dictionary<string, string> arguments = typeof(DevFlags)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.GetRawConstantValue() is string)
            .ToDictionary(
                field => field.Name,
                field => (string)(field.GetRawConstantValue() ?? string.Empty),
                StringComparer.Ordinal
            );
        var used = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<IReadOnlyList<string>>();
        foreach (PropertyInfo flag in ObjectTable.Properties(typeof(DevFlags)))
        {
            string argument;
            if (arguments.TryGetValue(flag.Name + "Argument", out string? name))
            {
                used.Add(flag.Name + "Argument");
                argument = name;
            }
            else if (arguments.TryGetValue(flag.Name + "Prefix", out string? prefix))
            {
                used.Add(flag.Name + "Prefix");
                argument = prefix + "…";
            }
            else
            {
                context.Report($"DevFlags.{flag.Name} has no {flag.Name}Argument or {flag.Name}Prefix constant.");
                continue;
            }

            rows.Add(
                MarkdownWriter.Row(
                    MarkdownWriter.Code(argument),
                    MarkdownWriter.Code(flag.Name),
                    context.Describe(flag) ?? ValueFormatter.None
                )
            );
        }

        foreach (string constant in arguments.Keys.Where(constant => !used.Contains(constant)))
        {
            context.Report($"DevFlags.{constant} matches no flag property.");
        }

        writer.Paragraph(
            "Pass them after "
                + MarkdownWriter.Code("--")
                + ", e.g. "
                + MarkdownWriter.Code("\"$GODOT_BIN\" --path . -- " + DevFlags.AutopilotArgument)
                + "."
        );
        writer.Table(["Argument", "Flag", "Description"], rows);
        yield return new GeneratedPage(Page, "Dev flags", TuningPage.Group, writer.ToString());
    }
}
