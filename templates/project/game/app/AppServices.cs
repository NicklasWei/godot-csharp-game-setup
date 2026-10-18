using __GAME__.Core.Content;
using __GAME__.Core.Development;
using __GAME__.Core.Diagnostics;
using __GAME__.Game.L10n;

namespace __GAME__.Game.App;

/// <summary>
/// The application-lifetime services built by <see cref="Main"/> and passed down to screens through their
/// <c>Initialize</c> methods. This is a parameter bundle, not a service locator: screens take what they need from it
/// at initialization and never store it globally.
/// </summary>
/// <param name="Log">Destination for diagnostic messages.</param>
/// <param name="Clock">Wall-clock and monotonic time.</param>
/// <param name="Locale">The active locale: translation and culture-aware formatting.</param>
/// <param name="Flags">Development switches from the command line.</param>
/// <param name="BuildVersion">The build version string shown to players.</param>
/// <param name="Content">Every validated content definition, loaded once at boot.</param>
public sealed record AppServices(
    ILog Log,
    IClock Clock,
    LocaleService Locale,
    DevFlags Flags,
    string BuildVersion,
    ContentDatabase Content
);
