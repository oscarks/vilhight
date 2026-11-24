using System;
using System.Collections.Generic;

namespace Rts.Sim.State;

public sealed class World
{
    internal World(WorldId id, StarSystemId starSystem, Country[] countries)
    {
        Id = id;
        StarSystem = starSystem;
        Countries = Array.AsReadOnly(countries);
    }

    public WorldId Id { get; }
    public StarSystemId StarSystem { get; }
    public IReadOnlyList<Country> Countries { get; }
}
