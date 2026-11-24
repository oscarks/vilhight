namespace Rts.Sim.State;

public readonly record struct PlayerId(int Value);
public readonly record struct StarSystemId(int Value);
public readonly record struct WorldId(int Value);
public readonly record struct CountryId(int Value);
public readonly record struct ResourceId(int Value); // índice denso; resolvido pelos registries na Fase 3
public readonly record struct TechId(int Value);     // índice denso; usado a partir da Fase 3
