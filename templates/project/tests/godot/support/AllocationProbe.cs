using Godot;

namespace __GAME__.RuntimeTests.Support;

/// <summary>
/// Measures main-thread managed allocations over a run of physics ticks, from inside the frame loop, so the
/// measurement covers every node's per-frame work and nothing the test itself does (Coding Standards §8).
/// </summary>
/// <remarks>
/// Add it to the tree and await <see cref="Completion"/> once. The test awaits nothing else meanwhile, because each
/// await allocates.
/// </remarks>
public sealed partial class AllocationProbe : Node
{
    /// <summary>Completes with the bytes allocated over the window.</summary>
    private readonly TaskCompletionSource<long> _completion = new();

    /// <summary>How many physics ticks to measure.</summary>
    private readonly int _ticks;

    /// <summary>Bytes allocated during each tick of the window.</summary>
    private readonly long[] _perTick;

    /// <summary>Main-thread allocated bytes at the start of the window.</summary>
    private long _start;

    /// <summary>Main-thread allocated bytes at the previous tick.</summary>
    private long _previous;

    /// <summary>Ticks seen so far.</summary>
    private int _seen;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllocationProbe"/> class.
    /// </summary>
    /// <param name="ticks">How many physics ticks to measure.</param>
    public AllocationProbe(int ticks)
    {
        _ticks = ticks;
        _perTick = new long[ticks + 1];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AllocationProbe"/> class. Required by Godot; measures 60 ticks.
    /// </summary>
    public AllocationProbe()
        : this(60) { }

    /// <summary>Gets a task that completes with the bytes allocated on the main thread during the window.</summary>
    public Task<long> Completion => _completion.Task;

    /// <summary>
    /// Describes which ticks allocated, for a failure message.
    /// </summary>
    /// <returns>Entries like <c>tick 12: 96 B</c>, or an empty string when none did.</returns>
    public string DescribeAllocatingTicks()
    {
        var entries = new List<string>();
        for (int tick = 1; tick < _perTick.Length; tick++)
        {
            if (_perTick[tick] != 0)
            {
                entries.Add($"tick {tick}: {_perTick[tick]} B");
            }
        }

        return string.Join(", ", entries);
    }

    /// <inheritdoc/>
    public override void _PhysicsProcess(double delta)
    {
        long now = GC.GetAllocatedBytesForCurrentThread();
        if (_seen == 0)
        {
            _start = now;
        }
        else
        {
            _perTick[_seen] = now - _previous;
        }

        _previous = now;
        if (_seen == _ticks)
        {
            SetPhysicsProcess(false);
            _completion.SetResult(now - _start);
        }

        _seen++;
    }
}
