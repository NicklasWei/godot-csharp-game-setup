using __GAME__.Core.Content;
using __GAME__.Core.Tests.TestSupport;

namespace __GAME__.Core.Tests.Content;

/// <summary>Tests for <see cref="ContentDatabase"/>.</summary>
public sealed class ContentDatabaseTests
{
    [Fact]
    public void TryGet_KnownIdAndType_FindsIt()
    {
        // Arrange
        var database = new ContentDatabase([new WidgetDefinition("widget.a", 1f)]);

        // Act
        bool found = database.TryGet("widget.a", out WidgetDefinition? widget);

        // Assert
        Assert.True(found);
        Assert.Equal(1f, widget?.Size);
    }

    [Fact]
    public void TryGet_UnknownId_IsFalse()
    {
        // Act & Assert
        Assert.False(ContentDatabase.Empty.TryGet("widget.none", out WidgetDefinition? _));
    }

    [Fact]
    public void Get_UnknownId_Throws()
    {
        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => ContentDatabase.Empty.Get<WidgetDefinition>("widget.none"));
    }

    [Fact]
    public void Constructor_DuplicateIds_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ContentDatabase([new WidgetDefinition("widget.a", 1f), new WidgetDefinition("widget.a", 2f)])
        );
    }

    [Fact]
    public void AllAndAllOf_AreOrderedById()
    {
        // Arrange
        var database = new ContentDatabase([new WidgetDefinition("widget.b", 1f), new WidgetDefinition("widget.a", 1f)]);

        // Act & Assert
        Assert.Equal(["widget.a", "widget.b"], database.All.Select(definition => definition.Id));
        Assert.Equal(["widget.a", "widget.b"], database.AllOf<WidgetDefinition>().Select(widget => widget.Id));
    }
}
