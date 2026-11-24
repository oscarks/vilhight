using System;
using Rts.Sim.Core;
using Xunit;

namespace Rts.Sim.Tests.Core;

public sealed class SimRandomTests
{
    [Fact]
    public void Seeding_MatchesSplitMix64()
    {
        Assert.Equal(
            (0xE220A8397B1DCDAFUL, 0x6E789E6AA1B965F4UL, 0x06C45D188009454FUL, 0xF88BB8A8724C81ECUL),
            new SimRandom(0).Snapshot());
        Assert.Equal(
            (0xBDD732262FEB6E95UL, 0x28EFE333B266F103UL, 0x47526757130F9F52UL, 0x581CE1FF0E4AE394UL),
            new SimRandom(42).Snapshot());
        Assert.Equal(
            (0xE4D971771B652C20UL, 0xE99FF867DBF682C9UL, 0x382FF84CB27281E9UL, 0x6D1DB36CCBA982D2UL),
            new SimRandom(ulong.MaxValue).Snapshot());

        var r = new SimRandom(0);
        Assert.Equal(r.Snapshot(), r.Snapshot());
        Assert.Equal(0x99EC5F36CB75F2B4UL, r.NextU64());

        foreach (var seed in new ulong[] { 0, 1, 42, ulong.MaxValue })
        {
            var s = new SimRandom(seed).Snapshot();
            Assert.NotEqual(0UL, s.S0 | s.S1 | s.S2 | s.S3);
        }
    }

    [Theory]
    [InlineData(0UL, new[] { 0x99EC5F36CB75F2B4UL, 0xBF6E1F784956452AUL, 0x1A5F849D4933E6E0UL, 0x6AA594F1262D2D2CUL })]
    [InlineData(42UL, new[] { 0x15780B2E0C2EC716UL, 0x6104D9866D113A7EUL, 0xAE17533239E499A1UL, 0xECB8AD4703B360A1UL })]
    [InlineData(ulong.MaxValue, new[] { 0x8F5520D52A7EAD08UL, 0xC476A018CAA1802DUL, 0x81DE31C0D260469EUL, 0xBF658D7E065F3C2FUL })]
    public void NextU64_MatchesXoshiro256StarStar(ulong seed, ulong[] expected)
    {
        var r = new SimRandom(seed);
        foreach (var e in expected)
        {
            Assert.Equal(e, r.NextU64());
        }
    }

    [Fact]
    public void SameSeed_SameSequence()
    {
        var a = new SimRandom(123456789);
        var b = new SimRandom(123456789);
        for (var i = 0; i < 1000; i++)
        {
            Assert.Equal(a.NextU64(), b.NextU64());
        }
    }

    private static ulong[] First8(ulong seed)
    {
        var r = new SimRandom(seed);
        var arr = new ulong[8];
        for (var i = 0; i < 8; i++)
        {
            arr[i] = r.NextU64();
        }

        return arr;
    }

    [Fact]
    public void DifferentSeeds_Diverge()
    {
        var seeds = new ulong[] { 0, 1, 42, ulong.MaxValue };
        for (var i = 0; i < seeds.Length; i++)
        {
            for (var j = i + 1; j < seeds.Length; j++)
            {
                Assert.NotEqual(First8(seeds[i]), First8(seeds[j]));
            }
        }
    }

    [Fact]
    public void NextInt_VectorsAndGuards()
    {
        var r = new SimRandom(42);
        var got = new int[8];
        for (var i = 0; i < 8; i++)
        {
            got[i] = r.NextInt(6);
        }

        Assert.Equal(new[] { 0, 0, 5, 5, 4, 0, 4, 3 }, got);

        var r1 = new SimRandom(42);
        Assert.Equal(0, r1.NextInt(1));
        Assert.Equal(0, r1.NextInt(1));
        Assert.Equal(0, r1.NextInt(1));

        var g = new SimRandom(5);
        var before = g.Snapshot();
        Assert.Throws<ArgumentOutOfRangeException>(() => g.NextInt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => g.NextInt(-1));
        Assert.Equal(before, g.Snapshot());
    }

    [Fact]
    public void NextInt_DistributionIsExactForSeed42()
    {
        var r = new SimRandom(42);
        var counts = new int[6];
        for (var i = 0; i < 60_000; i++)
        {
            var v = r.NextInt(6);
            Assert.InRange(v, 0, 5);
            counts[v]++;
        }

        Assert.Equal(new[] { 9802, 10053, 10182, 10023, 10038, 9902 }, counts);
    }

    [Fact]
    public void NextInt_LargeBound()
    {
        var r = new SimRandom(7);
        Assert.Equal(900426827, r.NextInt(int.MaxValue));
        Assert.Equal(1577101311, r.NextInt(int.MaxValue));
        Assert.Equal(1125130284, r.NextInt(int.MaxValue));
    }

    [Fact]
    public void NextFixed_InUnitInterval()
    {
        var r = new SimRandom(42);
        Assert.Equal(360188718L, r.NextFixed().Raw);
        Assert.Equal(1627707782L, r.NextFixed().Raw);
        Assert.Equal(2920764210L, r.NextFixed().Raw);
        Assert.Equal(3971525959L, r.NextFixed().Raw);

        var r2 = new SimRandom(99);
        for (var i = 0; i < 10_000; i++)
        {
            var v = r2.NextFixed();
            Assert.True(Fixed.Zero <= v && v < Fixed.One);
        }
    }

    [Fact]
    public void Seed0_IsValid()
    {
        var r = new SimRandom(0);
        var first = r.NextU64();
        var allEqual = true;
        for (var i = 0; i < 1000; i++)
        {
            if (r.NextU64() != first)
            {
                allEqual = false;
            }
        }

        Assert.False(allEqual);
    }
}
