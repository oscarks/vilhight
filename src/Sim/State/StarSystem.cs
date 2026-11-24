using System;
using System.Collections.Generic;

namespace Rts.Sim.State;

public sealed class StarSystem
{
    internal StarSystem(StarSystemId id, PlayerId owner, World[] worlds)
    {
        Id = id;
        Owner = owner;
        Worlds = Array.AsReadOnly(worlds);
    }

    public StarSystemId Id { get; }
    public PlayerId Owner { get; }
    public IReadOnlyList<World> Worlds { get; }
}
