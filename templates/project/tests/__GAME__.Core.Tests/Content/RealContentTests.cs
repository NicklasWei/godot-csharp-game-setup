using System.Text.Json;
using __GAME__.Core.Content;
using __GAME__.Core.Tests.TestSupport;

namespace __GAME__.Core.Tests.Content;

/// <summary>
/// Validates the repository's real <c>content/</c> folder against the registered kinds (<see cref="ContentKinds"/>),
/// and keeps the generated JSON Schemas in <c>content/schemas/</c> in sync with the content models.
/// </summary>
public sealed class RealContentTests
{
    [Fact]
    public void Load_RealContentFolder_HasNoErrors()
    {
        // Act
        ContentLoadResult result = RealContent.Load();

        // Assert
        Assert.Empty(result.Errors.Select(error => error.ToString()));
    }

    [Fact]
    public void ContentFolders_AreAllRegisteredKinds()
    {
        // Arrange
        HashSet<string> known = [.. ContentKinds.All.Select(kind => kind.Folder), ContentLoader.SchemaFolder];

        // Act
        string[] unknown =
        [
            .. Directory
                .EnumerateDirectories(RepoPaths.Content)
                .Select(Path.GetFileName)
                .OfType<string>()
                .Where(folder => !known.Contains(folder)),
        ];

        // Assert
        Assert.Empty(unknown);
    }

    [Fact]
    public void Kinds_UseStrictJsonOptions()
    {
        // Act
        string[] lax =
        [
            .. ContentKinds
                .All.Where(kind =>
                    kind.DocumentType.Options.UnmappedMemberHandling
                        != System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
                    || kind.DocumentType.Options.ReadCommentHandling != JsonCommentHandling.Skip
                    || !kind.DocumentType.Options.AllowTrailingCommas
                    || kind.DocumentType.Options.PropertyNamingPolicy != JsonNamingPolicy.CamelCase
                )
                .Select(kind => kind.Folder),
        ];

        // Assert
        Assert.Empty(lax);
    }

    [Fact]
    public void Kinds_ShareASchemaOnlyWithTheSameDocumentType()
    {
        // Act
        string[] conflicts =
        [
            .. ContentKinds
                .All.GroupBy(kind => kind.SchemaName, StringComparer.Ordinal)
                .Where(group => group.Select(kind => kind.DocumentType.Type).Distinct().Count() > 1)
                .Select(group => group.Key),
        ];

        // Assert
        Assert.Empty(conflicts);
    }

    [Fact]
    public void Schemas_MatchTheContentModels()
    {
        // Arrange
        bool update = Environment.GetEnvironmentVariable(ContentSchemas.UpdateVariable) == "1";
        var stale = new List<string>();

        // Act
        foreach (IContentKind kind in ContentKinds.All.DistinctBy(kind => kind.SchemaName))
        {
            string path = ContentSchemas.PathFor(kind.SchemaName);
            string expected = ContentSchemas.Generate(kind.SchemaName, kind.DocumentType);
            string actual = File.Exists(path) ? File.ReadAllText(path).ReplaceLineEndings("\n") : string.Empty;
            if (actual != expected)
            {
                if (update)
                {
                    File.WriteAllText(path, expected);
                }
                else
                {
                    stale.Add(Path.GetFileName(path));
                }
            }
        }

        // Assert
        Assert.True(
            stale.Count == 0,
            $"Stale schemas: {string.Join(", ", stale)}. Regenerate them with "
                + $"{ContentSchemas.UpdateVariable}=1 dotnet test --project tests/__GAME__.Core.Tests."
        );
    }

    [Fact]
    public void SchemaFolder_HoldsOnlyRegisteredSchemas()
    {
        // Arrange
        HashSet<string> expected = [.. ContentKinds.All.Select(kind => kind.SchemaName + ".schema.json"), ".gitkeep"];

        // Act
        string[] orphans =
        [
            .. Directory
                .EnumerateFiles(Path.Combine(RepoPaths.Content, ContentLoader.SchemaFolder))
                .Select(Path.GetFileName)
                .OfType<string>()
                .Where(file => !expected.Contains(file)),
        ];

        // Assert
        Assert.Empty(orphans);
    }

    [Fact]
    public void ContentFiles_PointAtTheirSchemas()
    {
        // Arrange
        var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        var wrong = new List<string>();

        // Act
        foreach (IContentKind kind in ContentKinds.All)
        {
            string folder = Path.Combine(RepoPaths.Content, kind.Folder);
            string expected = $"../{ContentLoader.SchemaFolder}/{kind.SchemaName}.schema.json";
            foreach (string file in Directory.Exists(folder) ? Directory.GetFiles(folder, "*.json") : [])
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file), options);
                bool declared = document.RootElement.TryGetProperty("$schema", out JsonElement schema);
                if (!declared || schema.GetString() != expected)
                {
                    wrong.Add($"{kind.Folder}/{Path.GetFileName(file)} (expected \"$schema\": \"{expected}\")");
                }
            }
        }

        // Assert
        Assert.Empty(wrong);
    }
}
