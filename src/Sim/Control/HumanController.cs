using System;
using System.Collections.Generic;
using Rts.Sim.Commands;
using Rts.Sim.State;

namespace Rts.Sim.Control;

// Entrada da apresentação. Não carimba nem valida o emissor: a autoridade é o Step (RF-03).
public sealed class HumanController : IController
{
    private readonly List<Command> _pending = new();

    public HumanController(PlayerId player)
    {
        Identity = new ControllerRef(ControllerKind.Human, player);
    }

    public ControllerRef Identity { get; }

    public int PendingCount => _pending.Count;

    public void Enqueue(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _pending.Add(command);
    }

    public void Collect(GameState state, CountryId country, long tick, List<Command> output)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(output);
        var keep = 0;
        for (var i = 0; i < _pending.Count; i++)
        {
            var cmd = _pending[i];
            if (cmd.Country == country)
            {
                output.Add(cmd);
            }
            else
            {
                _pending[keep++] = cmd;
            }
        }

        _pending.RemoveRange(keep, _pending.Count - keep);
    }
}
