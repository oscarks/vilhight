using Rts.Sim.State;

namespace Rts.Game;

// Setup de demonstração do M0. Espelha SetupFactory.OneSystemThreeCountries(42) dos testes, para que o hash
// do jogo seja comparável aos valores de referência. Mapas e fluxo de partida substituem isto (E1/E14).
public static class DemoSetup
{
    public const ulong Seed = 42;

    public static readonly PlayerId LocalPlayer = new(1);

    public static GameSetup Create() =>
        new(
            Seed,
            new[] { new PlayerSetup(LocalPlayer, "Player One") },
            new[]
            {
                new StarSystemSetup(
                    new StarSystemId(1),
                    LocalPlayer,
                    new[]
                    {
                        new WorldSetup(
                            new WorldId(1),
                            new[]
                            {
                                new CountrySetup(new CountryId(1), "Alpha", new ControllerRef(ControllerKind.Human, LocalPlayer)),
                                new CountrySetup(new CountryId(2), "Beta", new ControllerRef(ControllerKind.Ai, LocalPlayer)),
                                new CountrySetup(new CountryId(3), "Gamma", new ControllerRef(ControllerKind.Ai, LocalPlayer)),
                            }),
                    }),
            });
}
