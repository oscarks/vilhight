using System.Collections.Generic;
using System.Collections.ObjectModel;
using Rts.Sim.Commands;

namespace Rts.Sim.Core;

public sealed class SimEvents
{
    public const int InitialRejectionCapacity = 16;

    private readonly List<CommandRejected> _rejections = new(InitialRejectionCapacity);

    internal SimEvents()
    {
        CompletedTick = -1;
        Rejections = new ReadOnlyCollection<CommandRejected>(_rejections);
    }

    public long CompletedTick { get; private set; }

    public int AppliedCount { get; private set; }

    public IReadOnlyList<CommandRejected> Rejections { get; }

    internal void Begin(long tick)
    {
        _rejections.Clear();
        AppliedCount = 0;
        CompletedTick = tick;
    }

    internal void AddApplied() => AppliedCount++;

    internal void AddRejection(CommandRejected rejection) => _rejections.Add(rejection);
}
