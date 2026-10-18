using System.Text.Json.Nodes;
using __GAME__.Core.Tests.TestSupport;

namespace __GAME__.Core.Tests.Content;

/// <summary>Tests for <see cref="ContentSchemas"/>, the schema generator, using the test-only widget kind.</summary>
public sealed class ContentSchemasTests
{
    [Fact]
    public void Generate_WidgetDocument_DescribesItsPropertiesAndHeader()
    {
        // Act
        string text = ContentSchemas.Generate("widget", TestKinds.Widgets.DocumentType);
        JsonNode? schema = JsonNode.Parse(text);

        // Assert
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", schema?["$schema"]?.GetValue<string>());
        Assert.NotNull(schema?["properties"]?["size"]);
        Assert.NotNull(schema?["properties"]?["id"]);
        Assert.EndsWith("\n", text, StringComparison.Ordinal);
    }
}
