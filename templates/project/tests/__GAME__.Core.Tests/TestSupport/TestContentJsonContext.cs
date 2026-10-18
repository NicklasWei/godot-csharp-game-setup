using System.Text.Json;
using System.Text.Json.Serialization;

namespace __GAME__.Core.Tests.TestSupport;

/// <summary>
/// Source-generated JSON metadata for the test-only content documents, with the same options the game's
/// <c>ContentJsonContext</c> must use (see <c>ContentKinds</c>).
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
)]
[JsonSerializable(typeof(WidgetDocument))]
internal sealed partial class TestContentJsonContext : JsonSerializerContext;
