namespace __GAME__.Wiki;

/// <summary>
/// The wiki generator's entry point (ADR 0008).
/// </summary>
internal static class Program
{
    /// <summary>
    /// Generates the wiki pages.
    /// </summary>
    /// <param name="args">See <see cref="WikiOptions.Usage"/>.</param>
    /// <returns>0 on success, 1 when a strict run found problems or content failed to load, 2 for bad arguments.</returns>
    public static int Main(string[] args)
    {
        if (!WikiOptions.TryParse(args, out WikiOptions? options, out string? error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(WikiOptions.Usage);
            return 2;
        }

        return WikiGenerator.Run(options, Console.Out, Console.Error);
    }
}
