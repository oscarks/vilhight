using System;
using Rts.Sim.Core;
using Rts.Sim.State;
using Rts.Sim.Tests.Fixtures;
using Xunit;

namespace Rts.Sim.Tests.State;

public sealed class CountryStockTests
{
    // UT-BE-59
    [Fact]
    public void Country_Stock()
    {
        var state = GameState.Create(SetupFactory.OneSystemThreeCountries(), 5);
        var c = state.FindCountry(new CountryId(1))!;
        c.SetStock(new ResourceId(2), Fixed.FromInt(7));
        Assert.Equal(Fixed.FromInt(7), c.GetStock(new ResourceId(2)));
        Assert.Equal(Fixed.FromInt(7), c.Stock[2]);
        Assert.Equal(Fixed.Zero, c.Stock[1]);
        Assert.Equal(Fixed.Zero, state.FindCountry(new CountryId(2))!.GetStock(new ResourceId(2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => c.GetStock(new ResourceId(5)));
        Assert.Throws<ArgumentOutOfRangeException>(() => c.GetStock(new ResourceId(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => c.SetStock(new ResourceId(5), Fixed.One));

        var empty = GameState.Create(SetupFactory.OneSystemThreeCountries(), 0);
        Assert.Equal(0, empty.FindCountry(new CountryId(1))!.Stock.Length);
    }
}
