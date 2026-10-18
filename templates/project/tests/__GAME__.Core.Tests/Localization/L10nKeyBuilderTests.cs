using __GAME__.Core.Localization;

namespace __GAME__.Core.Tests.Localization;

/// <summary>Tests for <see cref="L10nKeyBuilder"/>.</summary>
public sealed class L10nKeyBuilderTests
{
    [Fact]
    public void Name_ContentId_AppendsName()
    {
        // Act & Assert
        Assert.Equal("item.iron_sword.name", L10nKeyBuilder.Name("item.iron_sword"));
    }

    [Fact]
    public void Description_ContentId_AppendsDesc()
    {
        // Act & Assert
        Assert.Equal("item.iron_sword.desc", L10nKeyBuilder.Description("item.iron_sword"));
    }

    [Fact]
    public void Name_EmptyId_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => L10nKeyBuilder.Name(string.Empty));
    }
}
