using System;
using System.Collections.Generic;
using System.Reflection;
using Rts.Sim.State;
using Rts.Sim.Tests.Fixtures;
using Xunit;

namespace Rts.Sim.Tests.State;

public sealed class StateEncapsulationTests
{
    // UT-BE-60
    [Fact]
    public void State_IsReadOnlyForPresentation()
    {
        Assert.False(typeof(GameState).GetProperty("Tick")!.SetMethod!.IsPublic);
        Assert.False(typeof(Country).GetProperty("Controller")!.SetMethod!.IsPublic);
        Assert.NotNull(typeof(GameState).GetProperty("Random", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.Null(typeof(GameState).GetProperty("Random"));
        Assert.Null(typeof(Country).GetMethod("SetStock"));
        foreach (var t in new[] { typeof(Country), typeof(World), typeof(StarSystem), typeof(Universe), typeof(Player) })
        {
            Assert.Empty(t.GetConstructors());
        }

        var state = GameState.Create(SetupFactory.OneSystemThreeCountries(), 1);
        var c = state.Countries[0];
        Assert.False(state.Countries is Country[]);
        Assert.Throws<NotSupportedException>(() => ((IList<Country>)state.Countries).Add(c));
        Assert.False(state.Players is Player[]);
        Assert.Throws<NotSupportedException>(() => ((IList<Player>)state.Players).Add(state.Players[0]));
        Assert.False(state.Universe.StarSystems is StarSystem[]);
        Assert.Throws<NotSupportedException>(
            () => ((IList<StarSystem>)state.Universe.StarSystems).Add(state.Universe.StarSystems[0]));
    }

    // UT-BE-61
    [Fact]
    public void Ids_AreStrongAndValueEqual()
    {
        Assert.Equal(new CountryId(1), new CountryId(1));
        Assert.NotEqual(new CountryId(1), new CountryId(2));
        Assert.False(((object)new CountryId(1)).Equals(new WorldId(1)));
    }
}
