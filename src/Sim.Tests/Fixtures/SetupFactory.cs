using System.Collections.Generic;
using Rts.Sim.State;

namespace Rts.Sim.Tests.Fixtures;

public static class SetupFactory
{
    public static CountrySetup Country(int id, string name, ControllerKind kind, int player) =>
        new(new CountryId(id), name, new ControllerRef(kind, new PlayerId(player)));

    public static GameSetup OneSystemThreeCountries(ulong seed = 42) =>
        new(
            seed,
            new[] { new PlayerSetup(new PlayerId(1), "Player One") },
            new[]
            {
                new StarSystemSetup(
                    new StarSystemId(1),
                    new PlayerId(1),
                    new[]
                    {
                        new WorldSetup(
                            new WorldId(1),
                            new[]
                            {
                                Country(1, "Alpha", ControllerKind.Human, 1),
                                Country(2, "Beta", ControllerKind.Ai, 1),
                                Country(3, "Gamma", ControllerKind.Ai, 1),
                            }),
                    }),
            });

    public static GameSetup TwoSystemsThreeCountries(ulong seed = 42) =>
        new(
            seed,
            new[]
            {
                new PlayerSetup(new PlayerId(1), "Player One"),
                new PlayerSetup(new PlayerId(2), "Player Two"),
            },
            new[]
            {
                new StarSystemSetup(
                    new StarSystemId(10),
                    new PlayerId(1),
                    new[]
                    {
                        new WorldSetup(
                            new WorldId(100),
                            new[]
                            {
                                Country(1, "Alpha", ControllerKind.Human, 1),
                                Country(3, "Gamma", ControllerKind.Ai, 1),
                            }),
                        new WorldSetup(new WorldId(101), new List<CountrySetup>()),
                    }),
                new StarSystemSetup(
                    new StarSystemId(20),
                    new PlayerId(2),
                    new[]
                    {
                        new WorldSetup(
                            new WorldId(200),
                            new[] { Country(2, "Beta", ControllerKind.Human, 2) }),
                    }),
            });

    public static GameSetup WorldWithCountries(int count)
    {
        var countries = new List<CountrySetup>();
        for (var id = 1; id <= count; id++)
        {
            countries.Add(Country(id, "C" + id, ControllerKind.Ai, 1));
        }

        return new GameSetup(
            42,
            new[] { new PlayerSetup(new PlayerId(1), "Player One") },
            new[]
            {
                new StarSystemSetup(
                    new StarSystemId(1),
                    new PlayerId(1),
                    new[] { new WorldSetup(new WorldId(1), countries) }),
            });
    }

    // Setup com um único mundo contendo os países dados (jogador 1, sistema 1, mundo 1).
    public static GameSetup WithCountries(params CountrySetup[] countries) =>
        new(
            42,
            new[] { new PlayerSetup(new PlayerId(1), "Player One") },
            new[]
            {
                new StarSystemSetup(
                    new StarSystemId(1),
                    new PlayerId(1),
                    new[] { new WorldSetup(new WorldId(1), countries) }),
            });
}
