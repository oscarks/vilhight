using System;
using Rts.Sim.Core;
using Rts.Sim.Hosting;
using Xunit;

namespace Rts.Sim.Tests.Hosting;

public sealed class TickClockTests
{
    [Fact]
    public void TickClock_InitialState()
    {
        // UT-BE-150
        var c = new TickClock();
        Assert.Equal(50_000L, TickClock.TickMicros);
        Assert.Equal(5, TickClock.MaxTicksPerAdvance);
        Assert.Equal(0, c.AccumulatedMicros);
        Assert.Equal(0, c.DroppedMicros);
        Assert.Equal(0, c.TotalTicks);
        Assert.Equal(Fixed.Zero, c.Alpha);
    }

    [Fact]
    public void Advance_AccumulatesAndCarries()
    {
        // UT-BE-151
        var c = new TickClock();
        Assert.Equal(0, c.Advance(49_999));
        Assert.Equal(49_999, c.AccumulatedMicros);
        Assert.Equal(1, c.Advance(1));
        Assert.Equal(0, c.AccumulatedMicros);
        Assert.Equal(2, c.Advance(125_000));
        Assert.Equal(25_000, c.AccumulatedMicros);
        Assert.Equal(2_147_483_648L, c.Alpha.Raw);
        Assert.Equal(0, c.Advance(0));
        Assert.Equal(25_000, c.AccumulatedMicros);
        Assert.Equal(3, c.TotalTicks);
        Assert.Equal(0, c.DroppedMicros);
    }

    [Fact]
    public void Advance_CapsAtFiveAndDropsExcess()
    {
        // UT-BE-152
        var c = new TickClock();
        Assert.Equal(5, c.Advance(260_000));
        Assert.Equal(10_000, c.AccumulatedMicros);
        Assert.Equal(0, c.DroppedMicros);
        Assert.Equal(5, c.Advance(300_000));
        Assert.Equal(60_000, c.DroppedMicros);
        Assert.Equal(0, c.AccumulatedMicros);
        Assert.Equal(5, c.Advance(10_000_000));
        Assert.Equal(60_000 + 9_750_000, c.DroppedMicros);
        Assert.Equal(0, c.AccumulatedMicros);
        Assert.Equal(15, c.TotalTicks);
        Assert.Equal(
            260_000L + 300_000 + 10_000_000,
            (c.TotalTicks * TickClock.TickMicros) + c.AccumulatedMicros + c.DroppedMicros);
    }

    [Fact]
    public void Advance_RejectsInvalidElapsed()
    {
        // UT-BE-153
        var c = new TickClock();
        c.Advance(30_000);
        foreach (var bad in new[] { -1L, long.MinValue, long.MaxValue - 50_000 + 1 })
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => c.Advance(bad));
            Assert.Equal("elapsedMicros", ex.ParamName);
        }

        Assert.Equal(30_000, c.AccumulatedMicros);
        Assert.Equal(0, c.DroppedMicros);
        Assert.Equal(0, c.TotalTicks);
        Assert.Equal(5, new TickClock().Advance(long.MaxValue - 50_000));
    }

    [Fact]
    public void SixtySecondsOfFrames_Yield1200Ticks()
    {
        // UT-BE-154
        var a = new TickClock();
        long sumA = 0;
        for (var i = 0; i < 3600; i++)
        {
            sumA += InRange(a.Advance(16_667));
        }

        Assert.Equal(1200, sumA);
        Assert.Equal(1_200, a.AccumulatedMicros);
        Assert.Equal(0, a.DroppedMicros);

        var b = new TickClock();
        long sumB = 0;
        for (var i = 0; i < 1500; i++)
        {
            sumB += InRange(b.Advance(7_000));
            sumB += InRange(b.Advance(33_000));
        }

        Assert.Equal(1200, sumB);
        Assert.Equal(0, b.AccumulatedMicros);
        Assert.Equal(0, b.DroppedMicros);

        var d = new TickClock();
        long sumC = 0;
        for (var i = 0; i < 1800; i++)
        {
            sumC += InRange(d.Advance(16_667));
        }

        sumC += InRange(d.Advance(1_000_000));
        for (var i = 0; i < 1800; i++)
        {
            sumC += InRange(d.Advance(16_667));
        }

        Assert.Equal(1205, sumC);
        Assert.Equal(750_600, d.DroppedMicros);
        Assert.Equal(600, d.AccumulatedMicros);
    }

    [Fact]
    public void Alpha_IsFractionOfNextTick()
    {
        // UT-BE-155
        var c = new TickClock();
        c.Advance(12_500);
        Assert.Equal(1_073_741_824L, c.Alpha.Raw);
        c.Advance(37_499);
        Assert.Equal(Fixed.FromRatio(49_999, 50_000).Raw, c.Alpha.Raw);
        Assert.True(c.Alpha < Fixed.One);
        c.Advance(1);
        Assert.Equal(Fixed.Zero, c.Alpha);
    }

    [Fact]
    public void Advance_DoesNotAllocate()
    {
        // UT-BE-156
        var c = new TickClock();
        long acc = 0;
        for (var i = 0; i < 100; i++)
        {
            c.Advance(16_667);
            acc += c.Alpha.Raw;
        }

        acc = 0;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            c.Advance(16_667);
            acc += c.Alpha.Raw;
        }

        var after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0, after - before);
        Assert.NotEqual(0, acc);
    }

    private static int InRange(int n)
    {
        Assert.InRange(n, 0, 5);
        return n;
    }
}
