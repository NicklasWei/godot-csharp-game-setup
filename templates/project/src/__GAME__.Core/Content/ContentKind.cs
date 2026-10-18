using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace __GAME__.Core.Content;

/// <summary>
/// A content kind built from a document type and a validator: the usual way to declare one in
/// <see cref="ContentKinds.All"/>.
/// </summary>
/// <typeparam name="TDocument">The raw JSON shape, registered on the source-generated <c>ContentJsonContext</c>.</typeparam>
/// <typeparam name="TDefinition">The validated, immutable definition game code uses.</typeparam>
/// <remarks>
/// Parsing is strict: the document type comes from <c>ContentJsonContext</c>, whose options make unknown properties
/// errors and allow comments and trailing commas (see <see cref="ContentKinds"/>; a test checks every kind's
/// options). Load-time only: it allocates freely.
/// </remarks>
public sealed class ContentKind<TDocument, TDefinition> : IContentKind
    where TDocument : ContentDocument
    where TDefinition : class, IContentDefinition
{
    /// <summary>The document type's source-generated metadata.</summary>
    private readonly JsonTypeInfo<TDocument> _documentType;

    /// <summary>Turns a document and its ID into a definition, adding a message for each problem.</summary>
    private readonly Func<TDocument, string, ICollection<string>, TDefinition?> _validate;

    /// <summary>Lists the localization keys a definition needs.</summary>
    private readonly Func<TDefinition, IEnumerable<string>> _localizationKeys;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentKind{TDocument, TDefinition}"/> class.
    /// </summary>
    /// <param name="folder">The snake_case folder under <c>content/</c>, e.g. <c>items</c>.</param>
    /// <param name="idPrefix">The snake_case ID prefix, e.g. <c>item</c>.</param>
    /// <param name="documentType">The document's metadata, e.g. <c>ContentJsonContext.Default.ItemDocument</c>.</param>
    /// <param name="validate">
    /// Validates a document whose ID is already known to be present: it gets the document, its ID, and a list to add
    /// problems to, and returns the definition (or <see langword="null"/> when there were problems).
    /// </param>
    /// <param name="localizationKeys">
    /// The keys a definition needs in <c>en.po</c> (see <see cref="Localization.L10nKeyBuilder"/>), or
    /// <see langword="null"/> for none.
    /// </param>
    /// <param name="schemaName">The schema name, or <see langword="null"/> to use <paramref name="idPrefix"/>.</param>
    /// <exception cref="ArgumentException">The folder, prefix, or schema name isn't snake_case.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="documentType"/> or <paramref name="validate"/> is null.</exception>
    public ContentKind(
        string folder,
        string idPrefix,
        JsonTypeInfo<TDocument> documentType,
        Func<TDocument, string, ICollection<string>, TDefinition?> validate,
        Func<TDefinition, IEnumerable<string>>? localizationKeys = null,
        string? schemaName = null
    )
    {
        RequireSnakeCase(folder, nameof(folder));
        RequireSnakeCase(idPrefix, nameof(idPrefix));
        ArgumentNullException.ThrowIfNull(documentType);
        ArgumentNullException.ThrowIfNull(validate);
        SchemaName = schemaName ?? idPrefix;
        RequireSnakeCase(SchemaName, nameof(schemaName));
        Folder = folder;
        IdPrefix = idPrefix;
        _documentType = documentType;
        _validate = validate;
        _localizationKeys = localizationKeys ?? NoKeys;
    }

    /// <inheritdoc/>
    public string Folder { get; }

    /// <inheritdoc/>
    public string IdPrefix { get; }

    /// <inheritdoc/>
    public string SchemaName { get; }

    /// <inheritdoc/>
    public JsonTypeInfo DocumentType => _documentType;

    /// <inheritdoc/>
    public Type DefinitionType => typeof(TDefinition);

    /// <inheritdoc/>
    public bool TryParse(
        string json,
        [NotNullWhen(true)] out IContentDefinition? definition,
        out IReadOnlyList<string> problems
    )
    {
        ArgumentNullException.ThrowIfNull(json);
        definition = null;
        var found = new List<string>();
        problems = found;

        TDocument? document;
        try
        {
            document = JsonSerializer.Deserialize(json, _documentType);
        }
        catch (JsonException exception)
        {
            found.Add("Invalid JSON: " + exception.Message);
            return false;
        }

        if (document is null)
        {
            found.Add("The document is empty.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(document.Id))
        {
            found.Add("\"id\" is required.");
            return false;
        }

        TDefinition? result = _validate(document, document.Id, found);
        if (result is null || found.Count > 0)
        {
            if (found.Count == 0)
            {
                found.Add("The validator rejected the document without saying why.");
            }

            return false;
        }

        definition = result;
        return true;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentException"><paramref name="definition"/> is not a <typeparamref name="TDefinition"/>.</exception>
    public IEnumerable<string> RequiredLocalizationKeys(IContentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return definition is TDefinition typed
            ? _localizationKeys(typed)
            : throw new ArgumentException(
                $"{definition.Id} is not a {typeof(TDefinition).Name}; ask its own kind.",
                nameof(definition)
            );
    }

    /// <summary>
    /// The localization keys of a kind with no player-facing text.
    /// </summary>
    /// <param name="definition">The definition (unused).</param>
    /// <returns>No keys.</returns>
    private static IEnumerable<string> NoKeys(TDefinition definition) => [];

    /// <summary>
    /// Throws unless a name is snake_case.
    /// </summary>
    /// <param name="value">The name.</param>
    /// <param name="parameter">The parameter it came from, for the message.</param>
    /// <exception cref="ArgumentException">The name isn't snake_case.</exception>
    private static void RequireSnakeCase(string value, string parameter)
    {
        if (!ContentLoader.IsSnakeCase(value))
        {
            throw new ArgumentException($"\"{value}\" must be snake_case.", parameter);
        }
    }
}
