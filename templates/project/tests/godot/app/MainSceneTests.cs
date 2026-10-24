using __GAME__.Game.App;
using __GAME__.Game.Ui.Title;
using __GAME__.RuntimeTests.Runner;

namespace __GAME__.RuntimeTests.App;

/// <summary>
/// The main scene (the composition root, <see cref="Main"/>) boots in the live tree: it builds the services, loads
/// the real content through <c>res://</c>, and shows the first screen, with no engine errors.
/// </summary>
public sealed class MainSceneTests
{
    [GodotTest]
    public static async Task Main_Boots_ToTheTitleScreen(GodotTestContext context)
    {
        // Arrange
        Main main = context.Instantiate<Main>("res://game/app/main.tscn");

        // Act
        context.Root.AddChild(main);
        await context.ProcessFrames(3);

        // Assert
        GodotAssert.True(
            main.GetChildren().OfType<TitleScreen>().Any(),
            "Main should show the title screen after boot."
        );
    }
}
