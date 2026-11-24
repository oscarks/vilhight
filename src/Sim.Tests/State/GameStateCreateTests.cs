using System.Linq;
using Rts.Sim.Core;
using Rts.Sim.State;
using Rts.Sim.Tests.Fixtures;
using Xunit;

namespace Rts.Sim.Tests.State;

public sealed class GameStateCreateTests
{
    // UT-BE-50
    [Fact]
    public void Create_OneSystemThreeCountries()
    {
        var state = GameState.Create(SetupFactory.OneSystemThreeCountries(), 5);
        Assert.Equal(0, state.Tick);
        Assert.Equal(42UL, state.Seed);
        Assert.Equal(5, state.ResourceCount);
        Assert.Single(state.Players);
        Assert.Single(state.Universe.StarSystems);
        Assert.Single(state.Universe.StarSystems[0].Worlds);
        Assert.Equal(3, state.Countries.Count);
        Assert.Equal(new[] { 1, 2, 3 }, state.Countries.Select(c => c.Id.Value).ToArray());
        Assert.Equal(
            new ControllerRef(ControllerKind.Human, new PlayerId(1)),
            state.FindCountry(new CountryId(1))!.Controller);
        foreach (var c in state.Countries)
        {
            Assert.Equal(5, c.Stock.Length);
            for (var i = 0; i < c.Stock.Length; i++)
            {
                Assert.Equal(Fixed.Zero, c.Stock[i]);
            }
        }

        Assert.Same(state.Countries[0], state.Universe.StarSystems[0].Worlds[0].Countries[0]);
    }

    // UT-BE-51
    [Fact]
    public void Create_TwoSystemsTwoPlayers()
    {
        var state = GameState.Create(SetupFactory.TwoSystemsThreeCountries(), 3);
        var systems = state.Universe.StarSystems;
        Assert.Equal(new[] { 10, 20 }, systems.Select(s => s.Id.Value).ToArray());
        Assert.Equal(new[] { 100, 101 }, systems[0].Worlds.Select(w => w.Id.Value).ToArray());
        Assert.Equal(new[] { 200 }, systems[1].Worlds.Select(w => w.Id.Value).ToArray());
        Assert.Empty(systems[0].Worlds[1].Countries);
        Assert.Equal(new[] { 1, 2, 3 }, state.Countries.Select(c => c.Id.Value).ToArray());
        var c2 = state.FindCountry(new CountryId(2))!;
        Assert.Equal(new WorldId(200), c2.World);
        Assert.Equal(new StarSystemId(20), systems[1].Worlds[0].StarSystem);
        Assert.Equal(new PlayerId(2), systems[1].Owner);
    }

    // UT-BE-52
    [Fact]
    public void Create_OrdersCanonically()
    {
        var original = GameState.Create(SetupFactory.TwoSystemsThreeCountries(), 3);
        var s = SetupFactory.TwoSystemsThreeCountries();
        var reversed = new GameSetup(
            s.Seed,
            s.Players.Reverse().ToArray(),
            s.StarSystems
                .Select(sys => new StarSystemSetup(
                    sys.Id,
                    sys.Owner,
                    sys.Worlds.Select(w => new WorldSetup(w.Id, w.Countries.Reverse().ToArray())).Reverse().ToArray()))
                .Reverse()
                .ToArray());
        var state = GameState.Create(reversed, 3);

        Assert.Equal(Ids(original), Ids(state));
        AssertStrictlyIncreasing(state.Players.Select(p => p.Id.Value));
        AssertStrictlyIncreasing(state.Countries.Select(c => c.Id.Value));
        AssertStrictlyIncreasing(state.Universe.StarSystems.Select(x => x.Id.Value));
        foreach (var sys in state.Universe.StarSystems)
        {
            AssertStrictlyIncreasing(sys.Worlds.Select(w => w.Id.Value));
            foreach (var w in sys.Worlds)
            {
                AssertStrictlyIncreasing(w.Countries.Select(c => c.Id.Value));
            }
        }
    }

    private static string Ids(GameState st) =>
        string.Join(
            "|",
            string.Join(",", st.Players.Select(p => p.Id.Value)),
            string.Join(",", st.Countries.Select(c => c.Id.Value)),
            string.Join(
                ";",
                st.Universe.StarSystems.Select(x =>
                    x.Id.Value + ":" + string.Join(
                        ",",
                        x.Worlds.Select(w => w.Id.Value + "=" + string.Join("/", w.Countries.Select(c => c.Id.Value)))))));

    private static void AssertStrictlyIncreasing(System.Collections.Generic.IEnumerable<int> values)
    {
        var arr = values.ToArray();
        for (var i = 0; i + 1 < arr.Length; i++)
        {
            Assert.True(arr[i] < arr[i + 1]);
        }
    }

    // UT-BE-53
    [Fact]
    public void Find_ReturnsNullForMissing()
    {
        var state = GameState.Create(SetupFactory.TwoSystemsThreeCountries(), 3);
        Assert.Null(state.FindCountry(new CountryId(99)));
        Assert.Null(state.FindPlayer(new PlayerId(99)));
        Assert.Null(state.FindCountry(new CountryId(-1)));
        Assert.Equal("Player Two", state.FindPlayer(new PlayerId(2))!.Name);
    }

    // UT-BE-62
    [Fact]
    public void Create_SeedsRandom()
    {
        var state = GameState.Create(SetupFactory.OneSystemThreeCountries(42), 1);
        Assert.Equal(new SimRandom(42).Snapshot(), state.Random.Snapshot());
        var again = GameState.Create(SetupFactory.OneSystemThreeCountries(42), 1);
        Assert.Equal(state.Random.Snapshot(), again.Random.Snapshot());
        var other = GameState.Create(SetupFactory.OneSystemThreeCountries(43), 1);
        Assert.NotEqual(state.Random.Snapshot(), other.Random.Snapshot());
    }

    // UT-BE-63
    [Fact]
    public void Lookups_DoNotAllocate()
    {
        var state = GameState.Create(SetupFactory.TwoSystemsThreeCountries(), 3);
        long sink = 0;

        long Run(int n)
        {
            long acc = 0;
            for (var i = 0; i < n; i++)
            {
                acc += state.FindCountry(new CountryId(i % 4)) is null ? 0 : 1;
                acc += state.FindPlayer(new PlayerId(i % 3)) is null ? 0 : 1;
                acc += state.Countries[0].GetStock(new ResourceId(i % 3)).Raw;
            }

            return acc;
        }

        sink += Run(100);
        var before = System.GC.GetAllocatedBytesForCurrentThread();
        sink += Run(10_000);
        var after = System.GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0, after - before);
        Assert.NotEqual(0, sink);
    }
}
