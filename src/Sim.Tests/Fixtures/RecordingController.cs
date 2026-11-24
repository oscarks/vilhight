using System;
using System.Collections.Generic;
using Rts.Sim.Commands;
using Rts.Sim.Control;
using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Tests.Fixtures;

// IController de teste: emite AddStock(país, Identity, 0, amount(país)) e registra países e ticks.
public sealed class RecordingController : IController
{
    private readonly Func<CountryId, Fixed> _amount;

    public RecordingController(ControllerRef identity, Func<CountryId, Fixed>? amount = null)
    {
        Identity = identity;
        _amount = amount ?? (id => Fixed.FromInt(id.Value));
    }

    public ControllerRef Identity { get; }

    public List<int> CalledCountries { get; } = new();

    public List<long> CalledTicks { get; } = new();

    public void Collect(GameState state, CountryId country, long tick, List<Command> output)
    {
        CalledCountries.Add(country.Value);
        CalledTicks.Add(tick);
        output.Add(new AddStockCommand(country, Identity, new ResourceId(0), _amount(country)));
    }
}
