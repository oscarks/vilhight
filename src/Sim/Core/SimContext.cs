namespace Rts.Sim.Core;

public sealed class SimContext
{
    internal SimContext(SimEvents events)
    {
        Events = events;
    }

    // Tick em execução (valor de State.Tick no início do Step).
    public long Tick { get; internal set; }

    public SimEvents Events { get; }
}
