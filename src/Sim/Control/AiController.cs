using System;
using System.Collections.Generic;
using Rts.Sim.Commands;
using Rts.Sim.State;

namespace Rts.Sim.Control;

// Stub: não emite nada até o E6 (doutrina e cadência).
public sealed class AiController : IController
{
    public AiController(PlayerId player)
    {
        Identity = new ControllerRef(ControllerKind.Ai, player);
    }

    public ControllerRef Identity { get; }

    public void Collect(GameState state, CountryId country, long tick, List<Command> output)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(output);
    }
}
