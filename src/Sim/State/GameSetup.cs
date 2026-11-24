using System.Collections.Generic;

namespace Rts.Sim.State;

public sealed record GameSetup(
    ulong Seed,
    IReadOnlyList<PlayerSetup> Players,
    IReadOnlyList<StarSystemSetup> StarSystems);

public sealed record PlayerSetup(PlayerId Id, string Name);

public sealed record StarSystemSetup(
    StarSystemId Id,
    PlayerId Owner,
    IReadOnlyList<WorldSetup> Worlds);

public sealed record WorldSetup(WorldId Id, IReadOnlyList<CountrySetup> Countries);

public sealed record CountrySetup(CountryId Id, string Name, ControllerRef Controller);
