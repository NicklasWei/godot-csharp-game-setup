using System.Runtime.CompilerServices;

namespace __GAME__.RuntimeTests.Runner;

/// <summary>
/// Parks the .NET finalizer thread for a <c>--gc-stress</c> run, and lets it run only between tests, a step at a time,
/// so whatever a test leaves for the GC waits through the next test collected but unfinalized.
/// </summary>
/// <remarks>
/// <para>
/// This holds open the window behind godot#112067 (ADR 0006). A <c>RefCounted</c> object held only by its C# wrapper
/// outlives the wrapper until the wrapper's finalizer runs. If native code references it again in that window, as a
/// load does when the resource cache still has it, Godot leaves its instance binding with a released weak handle: its
/// next reference logs <c>Handle is not initialized</c> from <c>SwapGCHandleForType</c>, and its free logs the same
/// from <c>SetGodotObjectPtr</c>. A normal run hits the window only when a collection lands just before such a
/// reference. A stress run holds it open for a whole test, so the test that makes the reference, or the one after it
/// when the error comes from the free, fails every time.
/// </para>
/// <para>
/// The thread parks in the finalizer of a latch object of the gate's own. What one collection finds is finalized
/// before what a later one finds (in any order within a collection), so a latch queued by one collection holds back
/// everything later collections find.
/// </para>
/// </remarks>
internal sealed class FinalizerGate : IDisposable
{
    /// <summary>How long the gate waits for the finalizer thread to reach a latch.</summary>
    private static readonly TimeSpan ParkTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Set once the finalizer thread waits in the current latch.</summary>
    private ManualResetEventSlim _parked = new();

    /// <summary>Set to let the finalizer thread out of the current latch.</summary>
    private ManualResetEventSlim _opened = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FinalizerGate"/> class.
    /// </summary>
    private FinalizerGate() { }

    /// <summary>
    /// Parks the finalizer thread, and returns once it waits in the gate's latch.
    /// </summary>
    /// <returns>The closed gate.</returns>
    /// <remarks>Close it while nothing waits to be finalized, or some of that may run before the thread parks.</remarks>
    public static FinalizerGate Close()
    {
        var gate = new FinalizerGate();
        Abandon(gate._parked, gate._opened);
        GC.Collect();
        WaitUntilParked(gate._parked);
        return gate;
    }

    /// <summary>
    /// Finalizes, between two tests, whatever was collected before, and parks the thread again for the next test, with
    /// whatever that freed up collected but left unfinalized.
    /// </summary>
    /// <remarks>
    /// This goes one link down each chain a test leaves: a scene's wrapper finalized here frees the scene, which leaves
    /// the wrappers of its materials collectable, and those wait through the next test, which may load the scene again.
    /// </remarks>
    public void Step()
    {
        // What the test left, queued behind the current latch; then the next latch, queued behind that.
        GC.Collect();
        ManualResetEventSlim parked = _parked;
        ManualResetEventSlim opened = _opened;
        _parked = new ManualResetEventSlim();
        _opened = new ManualResetEventSlim();
        Abandon(_parked, _opened);
        GC.Collect();

        // Finalize it all, up to the next latch.
        opened.Set();
        WaitUntilParked(_parked);
        parked.Dispose();
        opened.Dispose();

        // What those finalizers freed up, collected and left waiting.
        GC.Collect();
    }

    /// <summary>
    /// Opens the gate for good: lets the finalizer thread go, and waits until it has finalized everything collected
    /// while the gate was closed.
    /// </summary>
    public void Dispose()
    {
        _opened.Set();
        GC.WaitForPendingFinalizers();
        _parked.Dispose();
        _opened.Dispose();
    }

    /// <summary>
    /// Makes a latch and drops it, so the next collection queues its finalizer.
    /// </summary>
    /// <param name="parked">Set when the latch's finalizer starts.</param>
    /// <param name="opened">Waited on before the latch's finalizer returns.</param>
    /// <remarks>Not inlined, so no reference to the latch is left on the caller's stack when the GC runs.</remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Abandon(ManualResetEventSlim parked, ManualResetEventSlim opened) =>
        _ = new Latch(parked, opened);

    /// <summary>
    /// Waits for the finalizer thread to reach a latch.
    /// </summary>
    /// <param name="parked">The latch's parked event.</param>
    /// <exception cref="InvalidOperationException">The thread didn't reach it in time.</exception>
    private static void WaitUntilParked(ManualResetEventSlim parked)
    {
        if (!parked.Wait(ParkTimeout))
        {
            throw new InvalidOperationException("The finalizer thread didn't reach the gate.");
        }
    }

    /// <summary>
    /// The object whose finalizer parks the finalizer thread.
    /// </summary>
    /// <param name="parked">Set when the finalizer starts.</param>
    /// <param name="opened">Waited on before the finalizer returns.</param>
    private sealed class Latch(ManualResetEventSlim parked, ManualResetEventSlim opened)
    {
        /// <summary>
        /// Finalizes an instance of the <see cref="Latch"/> class.
        /// </summary>
        /// <remarks>Reports the finalizer thread parked, then holds it until the gate lets it go.</remarks>
        ~Latch()
        {
            parked.Set();
            opened.Wait();
        }
    }
}
