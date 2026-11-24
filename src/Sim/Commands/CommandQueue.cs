using System;
using System.Collections.Generic;

namespace Rts.Sim.Commands;

// E0-5: "comandos aplicados no tick pedido, em ordem estável".
public sealed class CommandQueue
{
    // Ordenada por (Tick, Sequence); Sequence cresce a cada Enqueue (INV-Q-01).
    private readonly List<Entry> _entries = new();
    private long _sequence;

    public int Count => _entries.Count;

    // -1 antes do primeiro TakeDue.
    public long LastTakenTick { get; private set; } = -1;

    public void Enqueue(long tick, Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (tick < 0 || tick <= LastTakenTick)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        var index = _entries.Count;
        while (index > 0 && _entries[index - 1].Tick > tick)
        {
            index--;
        }

        _entries.Insert(index, new Entry(tick, _sequence++, command));
    }

    public void TakeDue(long tick, List<Command> output)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (tick <= LastTakenTick)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        var n = 0;
        while (n < _entries.Count && _entries[n].Tick <= tick)
        {
            output.Add(_entries[n].Command);
            n++;
        }

        _entries.RemoveRange(0, n);
        LastTakenTick = tick;
    }

    private readonly record struct Entry(long Tick, long Sequence, Command Command);
}
