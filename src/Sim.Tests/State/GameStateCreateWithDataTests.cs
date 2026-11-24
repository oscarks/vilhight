using System;
using Rts.Sim.Data;
using Rts.Sim.State;
using Rts.Sim.Tests.Fixtures;
using Xunit;

namespace Rts.Sim.Tests.State;

public sealed class GameStateCreateWithDataTests
{
    [Fact]
    public void Create_WithDataSet_DelegatesToResourceCount()
    {
        // UT-BE-139
        var data = DataLoader.Parse(DataFixtures.Base());
        var setup = SetupFactory.OneSystemThreeCountries(42);
        var s = GameState.Create(setup, data);
        Assert.Equal(5, s.ResourceCount);
        Assert.Equal(5, s.Countries[0].Stock.Length);
        Assert.Equal(Refs.S0Hash, Rts.Sim.Core.StateHasher.Compute(s));
        Assert.Equal("data", Assert.Throws<ArgumentNullException>(() => GameState.Create(setup, (DataSet)null!)).ParamName);
        Assert.Equal("setup", Assert.Throws<ArgumentNullException>(() => GameState.Create(null!, data)).ParamName);
        var empty = DataLoader.Parse(DataFixtures.Raw("""{"resources":[]}""", DataFixtures.NoTechs));
        Assert.Equal(0, GameState.Create(setup, empty).ResourceCount);
    }
}
