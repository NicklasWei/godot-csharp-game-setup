namespace __GAME__.Core.Tests.TestSupport;

/// <summary>
/// Measures managed allocations for the hot-path allocation tests (Coding Standards §7.4).
/// </summary>
/// <remarks>
/// A steady-state allocation repeats on every run, but coverage instrumentation and JIT tiering occasionally
/// allocate once on a first-time path, which makes single measurements flaky in the coverage job. So this runs
/// the block up to <c>runs</c> times and reports the lowest count: a real per-call allocation is never zero.
/// </remarks>
internal static class Allocations
{
    /// <summary>
    /// Measures the bytes a block allocates on the current thread.
    /// </summary>
    /// <param name="block">The steady-state work to measure; warm it up first.</param>
    /// <param name="runs">The most runs to try before giving up on seeing zero.</param>
    /// <returns>The lowest byte count over the runs (0 as soon as one run allocates nothing).</returns>
    public static long Measure(Action block, int runs = 3)
    {
        long lowest = long.MaxValue;
        for (int run = 0; run < runs && lowest > 0; run++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            block();
            lowest = Math.Min(lowest, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        return lowest;
    }
}
