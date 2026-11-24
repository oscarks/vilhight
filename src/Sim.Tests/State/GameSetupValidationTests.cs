using System;
using System.Collections.Generic;
using Rts.Sim.State;
using Rts.Sim.Tests.Fixtures;
using Xunit;

namespace Rts.Sim.Tests.State;

public sealed class GameSetupValidationTests
{
    private static GameSetupException Fails(GameSetup setup, int resourceCount = 0) =>
        Assert.Throws<GameSetupException>(() => GameState.Create(setup, resourceCount));

    private static CountrySetup C(int id, string name = "X", ControllerKind kind = ControllerKind.Ai, int player = 1) =>
        SetupFactory.Country(id, name, kind, player);

    private static PlayerSetup P(int id, string name = "P") => new(new PlayerId(id), name);

    private static StarSystemSetup S(int id, int owner, params WorldSetup[] worlds) =>
        new(new StarSystemId(id), new PlayerId(owner), worlds);

    private static WorldSetup W(int id, params CountrySetup[] countries) => new(new WorldId(id), countries);

    private static GameSetup G(PlayerSetup[] players, params StarSystemSetup[] systems) => new(1, players, systems);

    [Fact]
    public void Create_CountryLimit()
    {
        var ok = GameState.Create(SetupFactory.WorldWithCountries(8), 0);
        Assert.Equal(8, ok.Universe.StarSystems[0].Worlds[0].Countries.Count);
        Assert.Equal(GameSetupError.TooManyCountries, Fails(SetupFactory.WorldWithCountries(9)).Error);
        GameState.Create(SetupFactory.WorldWithCountries(0), 0);
    }

    public static IEnumerable<object[]> DuplicateCases()
    {
        yield return new object[]
        {
            G(new[] { P(1), P(1) }, S(1, 1)),
            GameSetupError.DuplicatePlayerId,
        };
        yield return new object[]
        {
            G(new[] { P(1) }, S(1, 1), S(1, 1)),
            GameSetupError.DuplicateStarSystemId,
        };
        yield return new object[]
        {
            G(new[] { P(1) }, S(1, 1, W(100)), S(2, 1, W(100))),
            GameSetupError.DuplicateWorldId,
        };
        yield return new object[]
        {
            G(new[] { P(1) }, S(1, 1, W(100, C(1)), W(101, C(1)))),
            GameSetupError.DuplicateCountryId,
        };
    }

    [Theory]
    [MemberData(nameof(DuplicateCases))]
    public void Create_RejectsDuplicateIds(GameSetup setup, GameSetupError expected)
    {
        var ex = Fails(setup);
        Assert.Equal(expected, ex.Error);
        Assert.IsAssignableFrom<ArgumentException>(ex);
        Assert.StartsWith(expected + ":", ex.Message);
    }

    [Fact]
    public void Create_RejectsInvalidController()
    {
        Assert.Equal(
            GameSetupError.InvalidControllerKind,
            Fails(SetupFactory.WithCountries(C(1, kind: (ControllerKind)0))).Error);
        Assert.Equal(
            GameSetupError.InvalidControllerKind,
            Fails(SetupFactory.WithCountries(C(1, kind: (ControllerKind)3))).Error);
        Assert.Equal(
            GameSetupError.InvalidControllerKind,
            Fails(SetupFactory.WithCountries(new CountrySetup(new CountryId(1), "X", default))).Error);
        Assert.Equal(
            GameSetupError.UnknownControllerPlayer,
            Fails(SetupFactory.WithCountries(C(1, kind: ControllerKind.Human, player: 7))).Error);
    }

    public static IEnumerable<object[]> OtherCases()
    {
        yield return new object[] { SetupFactory.OneSystemThreeCountries(), -1, GameSetupError.NegativeResourceCount };
        yield return new object[] { G(Array.Empty<PlayerSetup>(), S(1, 1)), 0, GameSetupError.NoPlayers };
        yield return new object[] { G(new[] { P(1) }), 0, GameSetupError.NoStarSystems };
        yield return new object[] { G(new[] { P(-1) }, S(1, 1)), 0, GameSetupError.NegativeId };
        yield return new object[] { G(new[] { P(1) }, S(-1, 1)), 0, GameSetupError.NegativeId };
        yield return new object[] { G(new[] { P(1) }, S(1, 1, W(-1))), 0, GameSetupError.NegativeId };
        yield return new object[] { G(new[] { P(1) }, S(1, 1, W(1, C(-1)))), 0, GameSetupError.NegativeId };
        yield return new object[] { SetupFactory.WithCountries(C(1, "")), 0, GameSetupError.InvalidName };
        yield return new object[] { SetupFactory.WithCountries(C(1, "   ")), 0, GameSetupError.InvalidName };
        yield return new object[] { SetupFactory.WithCountries(C(1, new string('a', 65))), 0, GameSetupError.InvalidName };
        yield return new object[] { G(new[] { P(1, "") }, S(1, 1)), 0, GameSetupError.InvalidName };
        yield return new object[] { G(new[] { P(1) }, S(1, 9)), 0, GameSetupError.UnknownStarSystemOwner };
    }

    [Theory]
    [MemberData(nameof(OtherCases))]
    public void Create_RejectsOtherInvalidSetups(GameSetup setup, int resourceCount, GameSetupError expected)
    {
        Assert.Equal(expected, Fails(setup, resourceCount).Error);
    }

    [Fact]
    public void Create_AcceptsMaxLengthName_AndRejectsNulls()
    {
        GameState.Create(SetupFactory.WithCountries(C(1, new string('a', 64))), 0);
        Assert.Throws<ArgumentNullException>(() => GameState.Create(null!, 0));
        var nullPlayers = new GameSetup(1, null!, new[] { S(1, 1) });
        Assert.Throws<ArgumentNullException>(() => GameState.Create(nullPlayers, 0));
        Assert.Throws<ArgumentNullException>(
            () => GameState.Create(SetupFactory.WithCountries(new CountrySetup(new CountryId(1), null!, new ControllerRef(ControllerKind.Ai, new PlayerId(1)))), 0));
    }

    [Fact]
    public void Create_ValidationOrderIsDeterministic()
    {
        var nine = new List<CountrySetup> { C(1, "") };
        for (var i = 2; i <= 9; i++)
        {
            nine.Add(C(i));
        }

        Assert.Equal(GameSetupError.TooManyCountries, Fails(SetupFactory.WithCountries(nine.ToArray())).Error);

        Assert.Equal(
            GameSetupError.DuplicateCountryId,
            Fails(SetupFactory.WithCountries(C(1), C(1), C(5, kind: (ControllerKind)0))).Error);
        Assert.Equal(
            GameSetupError.InvalidControllerKind,
            Fails(SetupFactory.WithCountries(C(5, kind: (ControllerKind)0), C(1), C(1))).Error);

        Assert.Equal(
            GameSetupError.NegativeResourceCount,
            Fails(G(Array.Empty<PlayerSetup>(), S(1, 1)), -1).Error);

        var bad = SetupFactory.WithCountries(C(1, ""));
        var e1 = Fails(bad);
        var e2 = Fails(bad);
        Assert.Equal(e1.Error, e2.Error);
        Assert.Equal(e1.Message, e2.Message);
    }
}
