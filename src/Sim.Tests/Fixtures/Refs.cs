using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Tests.Fixtures;

// Atalhos de teste (abreviações da spec §11).
public static class Refs
{
    public static readonly ControllerRef Human1 = new(ControllerKind.Human, new PlayerId(1));
    public static readonly ControllerRef Ai1 = new(ControllerKind.Ai, new PlayerId(1));

    public const ulong S0Hash = 210089492672693724UL;

    public static GameState S0() => GameState.Create(SetupFactory.OneSystemThreeCountries(42), 5);

    public static Simulation NewSim(GameState state, params Rts.Sim.Systems.ISimSystem[] systems) =>
        new(state, systems, TestCommands.CreateRegistry());

    public static AddStockCommand AddStock(int c, ControllerRef issuer, int r, Fixed amount) =>
        new(new CountryId(c), issuer, new ResourceId(r), amount);

    public static RollCommand Roll(int c, ControllerRef issuer, int r, int max) =>
        new(new CountryId(c), issuer, new ResourceId(r), max);
}
