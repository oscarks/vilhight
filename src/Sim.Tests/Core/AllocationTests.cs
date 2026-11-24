using System;
using Rts.Sim.Core;
using Xunit;

namespace Rts.Sim.Tests.Core;

public sealed class AllocationTests
{
    private static long Loop(SimRandom rng, int n)
    {
        long acc = 0;
        for (var i = 0; i < n; i++)
        {
            var u = rng.NextU64();
            var k = rng.NextInt(1000);
            var f = rng.NextFixed();
            var x = (f + Fixed.One) * f / (Fixed.One + Fixed.One);
            var y = Fixed.Sqrt(x);
            acc += y.Raw + k + (long)(u & 1);
        }

        return acc;
    }

    [Fact]
    public void FixedAndRandom_DoNotAllocate()
    {
        var rng = new SimRandom(1);
        Loop(rng, 100);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var acc = Loop(rng, 10_000);
        var after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0, after - before);
        Assert.NotEqual(0, acc);
    }
}
