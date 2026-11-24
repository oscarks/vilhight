using System;
using System.Collections.Generic;
using System.Globalization;
using Rts.Sim.Commands;
using Rts.Sim.State;

namespace Rts.Sim.Control;

public sealed class ControllerSet
{
    private readonly GameState _state;
    private readonly IController?[] _byCountry;

    public ControllerSet(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
        _byCountry = new IController?[state.Countries.Count];
    }

    public void Assign(CountryId country, IController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        var index = IndexOf(country);
        if (index < 0)
        {
            throw new ArgumentException(
                "unknown country: " + country.Value.ToString(CultureInfo.InvariantCulture), nameof(country));
        }

        _byCountry[index] = controller;
    }

    public IController? Get(CountryId country)
    {
        var index = IndexOf(country);
        return index < 0 ? null : _byCountry[index];
    }

    // Países em ordem de CountryId (state.Countries já é ordenada por id).
    public void Collect(List<Command> output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var countries = _state.Countries;
        for (var i = 0; i < countries.Count; i++)
        {
            _byCountry[i]?.Collect(_state, countries[i].Id, _state.Tick, output);
        }
    }

    private int IndexOf(CountryId id)
    {
        var countries = _state.Countries;
        var lo = 0;
        var hi = countries.Count - 1;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var v = countries[mid].Id.Value;
            if (v == id.Value)
            {
                return mid;
            }

            if (v < id.Value)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return -1;
    }
}
