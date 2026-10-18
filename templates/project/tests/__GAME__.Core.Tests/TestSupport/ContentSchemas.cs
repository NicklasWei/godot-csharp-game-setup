using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using __GAME__.Core.Content;

namespace __GAME__.Core.Tests.TestSupport;

/// <summary>
/// Generates the editor JSON Schemas in <c>content/schemas/</c> from the content document models, so the schemas can
/// never drift from what the loader accepts (Coding Standards §3.7). Never edit a schema by hand.
/// </summary>
internal static class ContentSchemas
{
    /// <summary>The environment variable that makes the schema tests rewrite stale schema files.</summary>
    public const string UpdateVariable = "UPDATE_CONTENT_SCHEMAS";

    /// <summary>
    /// Gets the schema file path for a schema name.
    /// </summary>
    /// <param name="name">The schema name, e.g. <c>item</c>.</param>
    /// <returns>The absolute path.</returns>
    public static string PathFor(string name) =>
        Path.Combine(RepoPaths.Content, ContentLoader.SchemaFolder, name + ".schema.json");

    /// <summary>
    /// Generates a schema's text: a draft 2020-12 header, then the exported model schema.
    /// </summary>
    /// <param name="name">The schema name, e.g. <c>item</c>.</param>
    /// <param name="type">The document type's metadata.</param>
    /// <returns>The schema, indented, with LF line endings and a final newline.</returns>
    public static string Generate(string name, JsonTypeInfo type)
    {
        JsonObject model = JsonSchemaExporter.GetJsonSchemaAsNode(type).AsObject();
        var schema = new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["title"] = $"__TITLE__ {name}",
            ["$comment"] =
                "Generated from the C# content model by RealContentTests; do not edit. "
                + $"Regenerate with {UpdateVariable}=1 dotnet test --project tests/__GAME__.Core.Tests.",
        };
        foreach (string key in model.Select(pair => pair.Key).ToList())
        {
            JsonNode? value = model[key];
            model.Remove(key);
            schema[key] = value;
        }

        var options = new JsonSerializerOptions { WriteIndented = true, NewLine = "\n" };
        return schema.ToJsonString(options) + "\n";
    }
}
