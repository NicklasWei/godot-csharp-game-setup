using __GAME__.Core.Content;
using __GAME__.Core.Tests.TestSupport;

namespace __GAME__.Core.Tests.Content;

/// <summary>Tests for <see cref="ContentLoader"/> and <see cref="ContentKind{TDocument, TDefinition}"/>, using the test-only widget kind.</summary>
public sealed class ContentLoaderTests
{
    [Fact]
    public void Load_ValidFiles_IndexesThemById()
    {
        // Arrange
        ContentFile[] files = [TestKinds.WidgetFile("small", 1f), TestKinds.WidgetFile("large", 4.5f)];

        // Act
        ContentLoadResult result = ContentLoader.Load(files, TestKinds.All);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(2, result.Database.Count);
        Assert.Equal(4.5f, result.Database.Get<WidgetDefinition>("widget.large").Size);
    }

    [Fact]
    public void Load_CommentsAndTrailingCommas_AreAllowed()
    {
        // Arrange
        ContentFile file = new("widgets/commented.json", "{ // a comment\n \"id\": \"widget.commented\", \"size\": 2, }");

        // Act
        ContentLoadResult result = ContentLoader.Load([file], TestKinds.All);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Load_SchemaProperty_IsAllowed()
    {
        // Arrange
        ContentFile file = new(
            "widgets/declared.json",
            """{ "$schema": "../schemas/widget.schema.json", "id": "widget.declared", "size": 1 }"""
        );

        // Act
        ContentLoadResult result = ContentLoader.Load([file], TestKinds.All);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Load_UnknownProperty_IsAnError()
    {
        // Arrange
        ContentFile file = new("widgets/typo.json", """{ "id": "widget.typo", "sise": 1 }""");

        // Act
        ContentLoadResult result = ContentLoader.Load([file], TestKinds.All);

        // Assert
        ContentError error = Assert.Single(result.Errors);
        Assert.Equal("widgets/typo.json", error.Path);
        Assert.Contains("sise", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_ValidatorProblem_IsReportedAndTheFileLeftOut()
    {
        // Arrange
        ContentFile[] files = [TestKinds.WidgetFile("broken", -1f), TestKinds.WidgetFile("fine", 1f)];

        // Act
        ContentLoadResult result = ContentLoader.Load(files, TestKinds.All);

        // Assert
        ContentError error = Assert.Single(result.Errors);
        Assert.Equal("widgets/broken.json: \"size\" must be a positive number of meters.", error.ToString());
        Assert.Equal(1, result.Database.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{ \"size\": 1 }")]
    [InlineData("{ \"id\": ")]
    public void Load_EmptyBrokenOrIdlessDocument_IsAnError(string text)
    {
        // Arrange
        ContentFile file = new("widgets/bad.json", text);

        // Act
        ContentLoadResult result = ContentLoader.Load([file], TestKinds.All);

        // Assert
        Assert.Single(result.Errors);
        Assert.Equal(0, result.Database.Count);
    }

    [Fact]
    public void Load_IdNotMatchingFileName_IsRejected()
    {
        // Arrange
        ContentFile file = new("widgets/one.json", """{ "id": "widget.two", "size": 1 }""");

        // Act
        ContentLoadResult result = ContentLoader.Load([file], TestKinds.All);

        // Assert
        Assert.Contains("must match the file name", Assert.Single(result.Errors).Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("widgets/nested/deep.json", "directly inside a kind folder")]
    [InlineData("widgets/notes.txt", "Only .json files")]
    [InlineData("widgets/BadName.json", "snake_case")]
    [InlineData("gadgets/thing.json", "Unknown content folder")]
    [InlineData("loose.json", "directly inside a kind folder")]
    public void Load_MisplacedFile_IsRejected(string path, string expectedMessage)
    {
        // Arrange
        ContentFile file = new(path, "{}");

        // Act
        ContentLoadResult result = ContentLoader.Load([file], TestKinds.All);

        // Assert
        Assert.Contains(expectedMessage, Assert.Single(result.Errors).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_NoKindsRegistered_ExplainsThatInTheError()
    {
        // Arrange
        ContentFile file = TestKinds.WidgetFile("small", 1f);

        // Act
        ContentLoadResult result = ContentLoader.Load([file], []);

        // Assert
        Assert.Contains("no content kinds are registered", Assert.Single(result.Errors).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_SchemaFolder_IsSkipped()
    {
        // Arrange
        ContentFile file = new("schemas/widget.schema.json", "{}");

        // Act
        ContentLoadResult result = ContentLoader.Load([file], TestKinds.All);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(0, result.Database.Count);
    }

    [Fact]
    public void Load_Errors_AreInPathOrder()
    {
        // Arrange
        ContentFile[] files = [new("widgets/b.json", "{}"), new("widgets/a.json", "{}")];

        // Act
        ContentLoadResult result = ContentLoader.Load(files, TestKinds.All);

        // Assert
        Assert.Equal(["widgets/a.json", "widgets/b.json"], result.Errors.Select(error => error.Path));
    }

    [Fact]
    public void Load_TwoKindsSharingAFolder_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ContentLoader.Load([], [TestKinds.Widgets, TestKinds.Widgets]));
    }

    [Fact]
    public void Load_TwoKindsSharingAPrefix_ReportsTheDuplicateId()
    {
        // Arrange
        var gadgets = new ContentKind<WidgetDocument, WidgetDefinition>(
            "gadgets",
            "widget",
            TestContentJsonContext.Default.WidgetDocument,
            (document, id, _) => new WidgetDefinition(id, document.Size ?? 1f)
        );
        ContentFile[] files = [TestKinds.WidgetFile("same", 1f), new("gadgets/same.json", """{ "id": "widget.same" }""")];

        // Act
        ContentLoadResult result = ContentLoader.Load(files, [TestKinds.Widgets, gadgets]);

        // Assert
        Assert.Contains("already defined", Assert.Single(result.Errors).Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("snake_case", true)]
    [InlineData("a1_b2", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("1abc", false)]
    [InlineData("Upper", false)]
    [InlineData("has-dash", false)]
    public void IsSnakeCase_Name_MatchesThePattern(string? name, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, ContentLoader.IsSnakeCase(name));
    }

    [Fact]
    public void Constructor_NonSnakeCaseFolder_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ContentKind<WidgetDocument, WidgetDefinition>(
                "Widgets",
                "widget",
                TestContentJsonContext.Default.WidgetDocument,
                (_, id, _) => new WidgetDefinition(id, 1f)
            )
        );
    }

    [Fact]
    public void Kind_ExposesItsShape()
    {
        // Act & Assert
        Assert.Equal("widgets", TestKinds.Widgets.Folder);
        Assert.Equal("widget", TestKinds.Widgets.IdPrefix);
        Assert.Equal("widget", TestKinds.Widgets.SchemaName);
        Assert.Equal(typeof(WidgetDefinition), TestKinds.Widgets.DefinitionType);
        Assert.Equal(typeof(WidgetDocument), TestKinds.Widgets.DocumentType.Type);
    }

    [Fact]
    public void Kind_ValidatorReturningNullSilently_StillReportsAProblem()
    {
        // Arrange
        var silent = new ContentKind<WidgetDocument, WidgetDefinition>(
            "widgets",
            "widget",
            TestContentJsonContext.Default.WidgetDocument,
            (_, _, _) => null
        );

        // Act
        bool parsed = silent.TryParse("""{ "id": "widget.x" }""", out _, out IReadOnlyList<string> problems);

        // Assert
        Assert.False(parsed);
        Assert.Single(problems);
    }

    [Fact]
    public void RequiredLocalizationKeys_OwnDefinition_ListsTheKindsKeys()
    {
        // Act
        IEnumerable<string> keys = TestKinds.Widgets.RequiredLocalizationKeys(new WidgetDefinition("widget.a", 1f));

        // Assert
        Assert.Equal(["widget.a.name"], keys);
    }

    [Fact]
    public void RequiredLocalizationKeys_ForeignDefinition_Throws()
    {
        // Arrange
        var foreign = new OtherDefinition("other.thing");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TestKinds.Widgets.RequiredLocalizationKeys(foreign));
    }

    [Fact]
    public void RequiredLocalizationKeys_KindWithoutKeys_IsEmpty()
    {
        // Arrange
        var silent = new ContentKind<WidgetDocument, WidgetDefinition>(
            "widgets",
            "widget",
            TestContentJsonContext.Default.WidgetDocument,
            (_, id, _) => new WidgetDefinition(id, 1f)
        );

        // Act & Assert
        Assert.Empty(silent.RequiredLocalizationKeys(new WidgetDefinition("widget.a", 1f)));
    }

    /// <summary>A definition of some other kind.</summary>
    /// <param name="Id">The ID.</param>
    private sealed record OtherDefinition(string Id) : IContentDefinition;
}
