using System;
using System.Collections.Generic;

namespace Rts.Sim.State;

public sealed class Universe
{
    internal Universe(StarSystem[] starSystems)
    {
        StarSystems = Array.AsReadOnly(starSystems);
    }

    public IReadOnlyList<StarSystem> StarSystems { get; }
}
