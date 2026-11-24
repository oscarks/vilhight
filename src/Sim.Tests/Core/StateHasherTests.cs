using System;
using System.Linq;
using Rts.Sim.Core;
using Rts.Sim.State;
using Rts.Sim.Tests.Fixtures;
using Xunit;
using static Rts.Sim.Tests.Fixtures.Refs;

namespace Rts.Sim.Tests.Core;

public sealed class StateHasherTests
{
    [Fact]
    public void Fnv1a_Constants()
    {
        // UT-BE-100
        Assert.Equal(14695981039346656037UL, StateHasher.OffsetBasis);
        Assert.Equal(1099511628211UL, StateHasher.Prime);
    }

    [Fact]
    public void Compute_GoldenValue()
    {
        // UT-BE-101
        Assert.Equal(S0Hash, StateHasher.Compute(S0()));
        Assert.Throws<ArgumentNullException>(() => StateHasher.Compute(null!));
    }

    [Fact]
    public void Compute_IsCanonicalAndPure()
    {
        // UT-BE-102
        var a = GameState.Create(SetupFactory.TwoSystemsThreeCountries(), 3);
        var original = SetupFactory.TwoSystemsThreeCountries();
        var reversed = new GameSetup(
            original.Seed,
            original.Players.Reverse().ToArray(),
            original.StarSystems
                .Select(s => new StarSystemSetup(
                    s.Id,
                    s.Owner,
                    s.Worlds.Select(w => new WorldSetup(w.Id, w.Countries.Reverse().ToArray())).Reverse().ToArray()))
                .Reverse()
                .ToArray());
        var b = GameState.Create(reversed, 3);
        Assert.Equal(StateHasher.Compute(a), StateHasher.Compute(b));

        var s0 = S0();
        var snap = s0.Random.Snapshot();
        Assert.Equal(StateHasher.Compute(s0), StateHasher.Compute(s0));
        Assert.Equal(snap, s0.Random.Snapshot());
    }

    [Fact]
    public void Compute_IsSensitive()
    {
        // UT-BE-103
        Assert.Equal(
            6213880399494383774UL,
            StateHasher.Compute(GameState.Create(SetupFactory.OneSystemThreeCountries(43), 5)));

        var s = S0();
        s.FindCountry(new CountryId(1))!.SetStock(new ResourceId(2), Fixed.FromInt(7));
        Assert.Equal(2484201149338628319UL, StateHasher.Compute(s));

        s = S0();
        s.Random.NextU64();
        Assert.Equal(13100547816691564996UL, StateHasher.Compute(s));

        s = S0();
        s.Tick = 1;
        Assert.NotEqual(S0Hash, StateHasher.Compute(s));

        s = S0();
        s.FindCountry(new CountryId(2))!.Controller = Human1;
        Assert.NotEqual(S0Hash, StateHasher.Compute(s));

        Assert.NotEqual(
            S0Hash,
            StateHasher.Compute(GameState.Create(SetupFactory.OneSystemThreeCountries(42), 4)));
    }

    [Fact]
    public void SimulationComputeHash_DelegatesToHasher()
    {
        // UT-BE-104
        var state = S0();
        var sim = NewSim(state);
        Assert.Equal(StateHasher.Compute(state), sim.ComputeHash());
        for (var i = 0; i < 3; i++)
        {
            sim.Step(new Rts.Sim.Commands.Command[] { AddStock(1, Human1, i, Fixed.FromInt(i + 1)), Roll(2, Ai1, 0, 6) });
        }

        Assert.Equal(StateHasher.Compute(state), sim.ComputeHash());
    }
}
