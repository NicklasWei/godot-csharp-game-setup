using __GAME__.Core.Localization;
using __GAME__.Game.App;
using __GAME__.Game.Common;
using Godot;

namespace __GAME__.Game.Ui.Title;

/// <summary>
/// The title screen: the game's name (a localized scene label) and the build version (formatted in code). A
/// placeholder for the real front end, kept to show the patterns every screen follows: typed exports validated in
/// <c>_Ready</c>, dependencies through <see cref="Initialize"/> before <c>AddChild</c>, and keys instead of text.
/// </summary>
public sealed partial class TitleScreen : Control
{
    /// <summary>The label that shows the build version.</summary>
    [Export]
    private Label _buildLabel = null!;

    /// <summary>The services, set by <see cref="Initialize"/>.</summary>
    private AppServices? _services;

    /// <summary>
    /// Gives the screen its dependencies. Call before adding it to the tree.
    /// </summary>
    /// <param name="services">The application services.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public void Initialize(AppServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
    }

    /// <inheritdoc/>
    public override void _Ready()
    {
        NodeGuard.RequireExport(this, _buildLabel);
        NodeGuard.RequireInitialized(this, _services);
        _buildLabel.Text = _services.Locale.Format(L10nKeys.UiTitleBuild, _services.BuildVersion);
    }
}
