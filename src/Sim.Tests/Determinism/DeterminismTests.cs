using System.Linq;
using Rts.Sim.Commands;
using Rts.Sim.Core;
using Rts.Sim.State;
using Rts.Sim.Tests.Fixtures;
using Xunit;
using static Rts.Sim.Tests.Fixtures.Refs;

namespace Rts.Sim.Tests.Determinism;

public sealed class DeterminismTests
{
    [Fact]
    public void SameSeedSameCommands_SameHashEveryTick()
    {
        // E2E-BE-01
        var script = DeterminismScenario.BuildScript();
        var a = DeterminismScenario.Run(script);
        var b = DeterminismScenario.Run(DeterminismScenario.BuildScript());
        Assert.Equal(1515, script.Sum(l => l.Count));
        Assert.Equal(AddStock(1, Human1, 4, Fixed.FromInt(7)), script[0][0]);
        Assert.Single(script[0]);
        Assert.Equal(1001, a.Hashes.Length);
        Assert.Equal(S0Hash, a.Hashes[0]);
        for (var t = 0; t <= 1000; t++)
        {
            Assert.True(a.Hashes[t] == b.Hashes[t], "first divergent tick: " + t);
        }

        Assert.Equal(7140252220498885332UL, a.Hashes[1]);
        Assert.Equal(14189062733609157557UL, a.Hashes[100]);
        Assert.Equal(11368851551727198225UL, a.Hashes[1000]);
        Assert.Equal(1495, a.Applied);
        Assert.Equal(20, a.Rejected);
        Assert.Equal(1000, a.FinalState.Tick);

        var expected = new[]
        {
            new long[] { 1529008356576, 1468878815232, 1086626725888, 1258425417728, 1292785156096 },
            new long[] { 1425929141472, 1108101562368, 1331439861760, 1078036791296, 1254130450432 },
            new long[] { 1348619730144, 1172526071808, 1236950581248, 1181116006400, 1387274436608 },
        };
        for (var c = 1; c <= 3; c++)
        {
            var country = a.FinalState.FindCountry(new CountryId(c))!;
            for (var r = 0; r < 5; r++)
            {
                Assert.Equal(expected[c - 1][r], country.GetStock(new ResourceId(r)).Raw);
            }
        }
    }

    [Fact]
    public void NegativeControls()
    {
        // E2E-BE-02
        var a = DeterminismScenario.Run(DeterminismScenario.BuildScript());

        var script2 = DeterminismScenario.BuildScript();
        Assert.Equal(AddStock(2, Ai1, 4, Fixed.FromInt(9)), script2[503][1]);
        script2[503][1] = AddStock(2, Ai1, 4, Fixed.FromInt(10));
        var c = DeterminismScenario.Run(script2);
        for (var t = 0; t <= 503; t++)
        {
            Assert.Equal(a.Hashes[t], c.Hashes[t]);
        }

        for (var t = 504; t <= 1000; t++)
        {
            Assert.NotEqual(a.Hashes[t], c.Hashes[t]);
        }

        var d = DeterminismScenario.Run(DeterminismScenario.BuildScript(), stateSeed: 43);
        Assert.NotEqual(a.Hashes[1000], d.Hashes[1000]);

        var e = DeterminismScenario.Run(
            DeterminismScenario.BuildScript(),
            beforeFirstStep: s =>
            {
                var country = s.FindCountry(new CountryId(3))!;
                country.SetStock(new ResourceId(0), country.GetStock(new ResourceId(0)) + Fixed.FromRaw(1));
            });
        for (var t = 0; t <= 1000; t++)
        {
            Assert.NotEqual(a.Hashes[t], e.Hashes[t]);
        }
    }

    [Fact]
    public void CommandsSurviveSerializationInDeterminismRun()
    {
        // E2E-BE-03
        var codec = new CommandCodec(TestCommands.CreateRegistry());
        var script = DeterminismScenario.BuildScript();
        var viaJson = script
            .Select(list => list.Select(c => codec.Deserialize(codec.Serialize(c))).ToList())
            .ToArray();
        Assert.Equal(11368851551727198225UL, DeterminismScenario.Run(viaJson).Hashes[1000]);
    }
}
