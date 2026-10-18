namespace __GAME__.Core.Content;

/// <summary>
/// The registry of content kinds: every folder under <c>content/</c> the game loads (ADR 0007). Adding a kind is one
/// entry here plus its document and definition types; the loader, the schema and localization tests, and the wiki
/// all pick it up from this list.
/// </summary>
/// <remarks>
/// <para>To add a kind:</para>
/// <list type="number">
/// <item>Write a document class deriving from <see cref="ContentDocument"/>: nullable properties, XML docs with
/// units (the wiki and the generated schema show them).</item>
/// <item>Register it on the source-generated context, creating the context with the first kind:
/// <code>
/// [JsonSourceGenerationOptions(
///     PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
///     ReadCommentHandling = JsonCommentHandling.Skip,
///     AllowTrailingCommas = true,
///     UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
/// [JsonSerializable(typeof(ItemDocument))]
/// internal sealed partial class ContentJsonContext : JsonSerializerContext;
/// </code>
/// (A context with no registered types doesn't compile, so the template has none yet.)</item>
/// <item>Write an immutable definition record implementing <see cref="IContentDefinition"/>.</item>
/// <item>Add a <see cref="ContentKind{TDocument, TDefinition}"/> to <see cref="All"/>, with its validator and its
/// localization keys (<see cref="Localization.L10nKeyBuilder"/>).</item>
/// <item>Regenerate the schemas (see <c>RealContentTests</c> in the Core tests), add the keys to every PO file, and add content.</item>
/// </list>
/// </remarks>
public static class ContentKinds
{
    /// <summary>Gets every kind, in a fixed order (the wiki lists them in this order).</summary>
    public static IReadOnlyList<IContentKind> All { get; } = [];
}
