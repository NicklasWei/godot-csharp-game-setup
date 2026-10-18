using System.Text.Json.Serialization;

namespace __GAME__.Core.Content;

/// <summary>
/// The base of every content document: the raw JSON shape of a content file, before validation. Each kind's
/// document derives from it and adds its own properties, all nullable, so a missing value is reported by the
/// kind's validator with a clear message rather than thrown by the deserializer.
/// </summary>
/// <remarks>
/// Documents are transient: a kind's validator turns one into an immutable <see cref="IContentDefinition"/>, and
/// game code never sees the document. The JSON Schemas in <c>content/schemas/</c> are generated from these types.
/// </remarks>
public abstract class ContentDocument
{
    /// <summary>Gets or sets the editor's schema reference (<c>$schema</c>); ignored when loading.</summary>
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    /// <summary>Gets or sets the permanent ID; the loader checks that it matches the file name.</summary>
    public string? Id { get; set; }
}
