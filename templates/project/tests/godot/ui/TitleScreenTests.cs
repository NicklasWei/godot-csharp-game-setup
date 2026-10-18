using __GAME__.Game.Ui.Title;
using __GAME__.RuntimeTests.Runner;
using __GAME__.RuntimeTests.Support;
using Godot;

namespace __GAME__.RuntimeTests.Ui;

/// <summary>
/// The title screen shows the build version, and idles at 0 B/frame: the template of every screen's allocation test.
/// </summary>
public sealed class TitleScreenTests
{
    /// <summary>The title scene.</summary>
    private const string ScenePath = "res://game/ui/title/title_screen.tscn";

    [GodotTest]
    public static async Task TitleScreen_ShowsTheBuildVersion(GodotTestContext context)
    {
        // Arrange
        TitleScreen title = GodotTestContext.Instantiate<TitleScreen>(ScenePath);
        title.Initialize(TestServices.Create());

        // Act
        context.Root.AddChild(title);
        await context.ProcessFrames(1);

        // Assert
        string text = title.GetNode<Label>("BuildLabel").Text;
        GodotAssert.True(text.Contains(TestServices.BuildVersion, StringComparison.Ordinal), $"Build label: {text}");
    }

    [GodotTest]
    public static async Task TitleScreen_Idle_AllocatesNothingPerFrame(GodotTestContext context)
    {
        // Arrange
        TitleScreen title = GodotTestContext.Instantiate<TitleScreen>(ScenePath);
        title.Initialize(TestServices.Create());
        context.Root.AddChild(title);
        await context.PhysicsFrames(10);
        var probe = new AllocationProbe(120);

        // Act
        context.Root.AddChild(probe);
        long allocated = await probe.Completion;

        // Assert
        GodotAssert.True(allocated == 0, $"Allocated {allocated} B: {probe.DescribeAllocatingTicks()}");
    }
}
