namespace __GAME__.Core.Development;

/// <summary>
/// Development switches passed on the command line after <c>--</c>
/// (<c>"$GODOT_BIN" --path . -- --autopilot</c>). They speed up iteration, automated captures, and CI runs, and do
/// nothing in normal play. They live in Core so parsing is unit-tested and the wiki can list them.
/// </summary>
/// <param name="Autopilot">
/// Drive the game with scripted input instead of the player's (<c>--autopilot</c>), so CI and the movie writer can
/// exercise gameplay without a person.
/// </param>
/// <param name="Pseudolocalize">
/// Turn on Godot's pseudolocalization (<c>--pseudoloc</c>) to catch hardcoded strings and text that overflows.
/// </param>
public sealed record DevFlags(bool Autopilot = false, bool Pseudolocalize = false)
{
    /// <summary>The argument that enables <see cref="Autopilot"/>.</summary>
    public const string AutopilotArgument = "--autopilot";

    /// <summary>The argument that enables <see cref="Pseudolocalize"/>.</summary>
    public const string PseudolocalizeArgument = "--pseudoloc";

    /// <summary>Gets the flags for normal play: every switch off.</summary>
    public static DevFlags None { get; } = new();

    /// <summary>
    /// Parses the user arguments.
    /// </summary>
    /// <param name="userArguments">The arguments after <c>--</c>, usually <c>OS.GetCmdlineUserArgs()</c>.</param>
    /// <param name="unknown">The arguments that aren't dev flags, in order, for the caller to warn about.</param>
    /// <returns>The flags.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="userArguments"/> is <see langword="null"/>.</exception>
    public static DevFlags Parse(IReadOnlyList<string> userArguments, out IReadOnlyList<string> unknown)
    {
        ArgumentNullException.ThrowIfNull(userArguments);
        DevFlags flags = None;
        var unrecognized = new List<string>();
        foreach (string argument in userArguments)
        {
            switch (argument)
            {
                case AutopilotArgument:
                    flags = flags with { Autopilot = true };
                    break;
                case PseudolocalizeArgument:
                    flags = flags with { Pseudolocalize = true };
                    break;
                default:
                    unrecognized.Add(argument);
                    break;
            }
        }

        unknown = unrecognized;
        return flags;
    }
}
