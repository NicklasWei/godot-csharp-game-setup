using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace __GAME__.Wiki.Sources;

/// <summary>
/// Core's XML documentation file (built next to the assembly), read as Markdown. The build requires a summary on
/// every member, with units, so these are the descriptions the wiki shows.
/// </summary>
internal sealed partial class XmlDocs
{
    private readonly Dictionary<string, XElement> _members;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocs"/> class.
    /// </summary>
    /// <param name="members">The <c>member</c> elements, keyed by their documentation ID (<c>P:Ns.Type.Name</c>).</param>
    private XmlDocs(Dictionary<string, XElement> members) => _members = members;

    /// <summary>
    /// Reads the documentation file of an assembly.
    /// </summary>
    /// <param name="assembly">The assembly; its <c>.xml</c> file must sit next to it.</param>
    /// <returns>The docs, or <see langword="null"/> when the file is missing.</returns>
    public static XmlDocs? TryLoad(Assembly assembly)
    {
        string path = Path.ChangeExtension(assembly.Location, ".xml");
        if (!File.Exists(path))
        {
            return null;
        }

        var members = new Dictionary<string, XElement>(StringComparer.Ordinal);
        foreach (XElement member in XDocument.Load(path).Descendants("member"))
        {
            if (member.Attribute("name")?.Value is { } name)
            {
                members[name] = member;
            }
        }

        return new XmlDocs(members);
    }

    /// <summary>
    /// Gets a type's summary.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The summary as Markdown, or <see langword="null"/>.</returns>
    public string? Summary(Type type) => Text(Member("T:" + Id(type))?.Element("summary"));

    /// <summary>
    /// Gets a property's summary, falling back to the declaring record's <c>param</c> doc for positional records.
    /// "Gets the speed." reads as "The speed.", since the table already says it is a value.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The summary as Markdown, or <see langword="null"/>.</returns>
    public string? Summary(PropertyInfo property)
    {
        Type owner = property.DeclaringType ?? throw new ArgumentException("No declaring type.", nameof(property));
        string? own = Text(Member("P:" + Id(owner) + "." + property.Name)?.Element("summary"));
        string? text =
            own
            ?? Text(
                Member("T:" + Id(owner))
                    ?.Elements("param")
                    .FirstOrDefault(param => param.Attribute("name")?.Value == property.Name)
            );
        return text is null ? null : WithoutGetsPrefix(text);
    }

    /// <summary>
    /// Gets a field's or constant's summary.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>The summary as Markdown, or <see langword="null"/>.</returns>
    public string? Summary(FieldInfo field)
    {
        Type owner = field.DeclaringType ?? throw new ArgumentException("No declaring type.", nameof(field));
        return Text(Member("F:" + Id(owner) + "." + field.Name)?.Element("summary"));
    }

    /// <summary>
    /// Gets an enum member's summary.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <returns>The summary as Markdown, or <see langword="null"/>.</returns>
    public string? Summary(Enum member) => Text(Member("F:" + Id(member.GetType()) + "." + member)?.Element("summary"));

    /// <summary>
    /// Gets a documentation ID's type part: the full name with nested types joined by dots.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The ID part, e.g. <c>__GAME__.Core.Content.ContentLoader</c>.</returns>
    private static string Id(Type type)
    {
        string name = (type.FullName ?? type.Name).Replace('+', '.');
        int generic = name.IndexOf('[', StringComparison.Ordinal);
        return generic < 0 ? name : name[..generic];
    }

    /// <summary>
    /// Rewrites "Gets the X" and "Gets a value indicating whether X" into "The X" and "Whether X".
    /// </summary>
    /// <param name="text">A property summary.</param>
    /// <returns>The rewritten summary.</returns>
    private static string WithoutGetsPrefix(string text)
    {
        const string Whether = "Gets a value indicating whether ";
        if (text.StartsWith(Whether, StringComparison.Ordinal))
        {
            return "Whether " + text[Whether.Length..];
        }

        foreach (string prefix in (string[])["Gets or sets ", "Gets "])
        {
            if (text.StartsWith(prefix, StringComparison.Ordinal) && text.Length > prefix.Length)
            {
                return char.ToUpperInvariant(text[prefix.Length]) + text[(prefix.Length + 1)..];
            }
        }

        return text;
    }

    /// <summary>
    /// Converts a doc element to one line of Markdown: <c>c</c> becomes code, <c>see</c> becomes the member's short
    /// name, and paragraphs and list items run together.
    /// </summary>
    /// <param name="element">The element, or <see langword="null"/>.</param>
    /// <returns>The text, or <see langword="null"/> when there is none.</returns>
    private static string? Text(XElement? element)
    {
        if (element is null)
        {
            return null;
        }

        var text = new StringBuilder();
        AppendNodes(element, text);
        string result = Whitespace().Replace(text.ToString(), " ").Trim();
        return result.Length == 0 ? null : result;
    }

    /// <summary>
    /// Appends the Markdown for an element's child nodes.
    /// </summary>
    /// <param name="element">The parent element.</param>
    /// <param name="text">The output.</param>
    private static void AppendNodes(XElement element, StringBuilder text)
    {
        foreach (XNode node in element.Nodes())
        {
            switch (node)
            {
                case XText plain:
                    text.Append(plain.Value);
                    break;
                case XElement { Name.LocalName: "c" } code:
                    text.Append('`').Append(code.Value).Append('`');
                    break;
                case XElement { Name.LocalName: "see" or "seealso" } see:
                    text.Append('`').Append(ReferenceName(see)).Append('`');
                    break;
                case XElement { Name.LocalName: "paramref" or "typeparamref" } reference:
                    text.Append('`').Append(reference.Attribute("name")?.Value).Append('`');
                    break;
                case XElement { Name.LocalName: "b" } bold:
                    text.Append("**").Append(bold.Value).Append("**");
                    break;
                case XElement child:
                    text.Append(' ');
                    AppendNodes(child, text);
                    text.Append(' ');
                    break;
            }
        }
    }

    /// <summary>
    /// Gets the short name a <c>see</c> element points at: the keyword, or the last part of the member ID.
    /// </summary>
    /// <param name="see">The element.</param>
    /// <returns>The name, e.g. <c>null</c> or <c>ContentLoader.Load</c>.</returns>
    private static string ReferenceName(XElement see)
    {
        if (see.Attribute("langword")?.Value is { } keyword)
        {
            return keyword;
        }

        string cref = see.Attribute("cref")?.Value ?? see.Value;
        int parameters = cref.IndexOf('(', StringComparison.Ordinal);
        string name = parameters < 0 ? cref : cref[..parameters];
        name = name.Length > 2 && name[1] == ':' ? name[2..] : name;

        // Keep the type with a member (ContentLoader.Load); a bare type keeps only its own name.
        string[] parts = name.Split('.');
        return cref.StartsWith("T:", StringComparison.Ordinal) || parts.Length < 2
            ? parts[^1]
            : parts[^2] + "." + parts[^1];
    }

    /// <summary>Matches runs of whitespace, which doc comments wrap across lines.</summary>
    /// <returns>The regex.</returns>
    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    /// <summary>
    /// Finds a member element.
    /// </summary>
    /// <param name="id">The documentation ID.</param>
    /// <returns>The element, or <see langword="null"/>.</returns>
    private XElement? Member(string id) => _members.GetValueOrDefault(id);
}
