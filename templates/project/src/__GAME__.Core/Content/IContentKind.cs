using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace __GAME__.Core.Content;

/// <summary>
/// One kind of content: a folder of JSON files sharing one document type, each validated into one definition.
/// Kinds are listed in <see cref="ContentKinds.All"/>; the loader, the schema and localization tests, and the wiki
/// all work from that list, so a new kind needs no other registration.
/// </summary>
public interface IContentKind
{
    /// <summary>Gets the folder under <c>content/</c> that holds this kind's files, e.g. <c>items</c>.</summary>
    string Folder { get; }

    /// <summary>
    /// Gets the ID prefix, e.g. <c>item</c>: the file <c>items/iron_sword.json</c> must hold <c>item.iron_sword</c>.
    /// </summary>
    string IdPrefix { get; }

    /// <summary>
    /// Gets the name of the kind's generated JSON Schema, <c>content/schemas/&lt;name&gt;.schema.json</c>. Kinds that
    /// share a document type may share a schema.
    /// </summary>
    string SchemaName { get; }

    /// <summary>Gets the source-generated JSON metadata of the document type, for parsing and schema export.</summary>
    JsonTypeInfo DocumentType { get; }

    /// <summary>Gets the type of the definitions this kind produces, for reflection (wiki, tests).</summary>
    Type DefinitionType { get; }

    /// <summary>
    /// Parses and validates one file's text.
    /// </summary>
    /// <param name="json">The whole file.</param>
    /// <param name="definition">The definition, when the file is valid.</param>
    /// <param name="problems">Every problem found; empty on success.</param>
    /// <returns><see langword="true"/> when the file produced a valid definition.</returns>
    bool TryParse(
        string json,
        [NotNullWhen(true)] out IContentDefinition? definition,
        out IReadOnlyList<string> problems
    );

    /// <summary>
    /// Lists the localization keys a definition of this kind needs in <c>en.po</c>, such as its name and
    /// description. The localization tests require each one, and the wiki shows their English text.
    /// </summary>
    /// <param name="definition">A definition this kind produced.</param>
    /// <returns>The keys; empty for a kind with no player-facing text.</returns>
    IEnumerable<string> RequiredLocalizationKeys(IContentDefinition definition);
}
