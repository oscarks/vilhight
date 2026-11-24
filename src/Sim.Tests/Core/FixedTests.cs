using System;
using Rts.Sim.Core;
using Xunit;

namespace Rts.Sim.Tests.Core;

public sealed class FixedTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(12345)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void FromInt_IsExact(int v)
    {
        Assert.Equal((long)v << 32, Fixed.FromInt(v).Raw);
        Assert.Equal(v, Fixed.FromInt(v).ToIntFloor());
    }

    [Fact]
    public void Constants_HaveExpectedRaw()
    {
        Assert.Equal(0, Fixed.Zero.Raw);
        Assert.Equal(4294967296, Fixed.One.Raw);
        Assert.Equal(long.MinValue, Fixed.MinValue.Raw);
        Assert.Equal(long.MaxValue, Fixed.MaxValue.Raw);
        Assert.Equal(123, Fixed.FromRaw(123).Raw);
        Assert.Equal(32, Fixed.FractionBits);
    }

    [Theory]
    [InlineData(1L, 3L, 1431655765L)]
    [InlineData(-1L, 3L, -1431655765L)]
    [InlineData(1L, -3L, -1431655765L)]
    [InlineData(7L, 2L, 15032385536L)]
    [InlineData(0L, 5L, 0L)]
    public void FromRatio_TruncatesTowardZero(long num, long den, long raw)
    {
        Assert.Equal(raw, Fixed.FromRatio(num, den).Raw);
    }

    [Fact]
    public void FromRatio_Errors()
    {
        Assert.Throws<DivideByZeroException>(() => Fixed.FromRatio(1, 0));
        Assert.Throws<OverflowException>(() => Fixed.FromRatio(long.MaxValue, 1));
    }

    [Fact]
    public void ToIntFloor_IsFloor()
    {
        Assert.Equal(3, Fixed.FromRatio(7, 2).ToIntFloor());
        Assert.Equal(-1, Fixed.FromRatio(-1, 2).ToIntFloor());
        Assert.Equal(-1, Fixed.FromRaw(-1).ToIntFloor());
        Assert.Equal(0, Fixed.FromRaw(1).ToIntFloor());
        Assert.Equal(int.MinValue, Fixed.MinValue.ToIntFloor());
        Assert.Equal(int.MaxValue, Fixed.MaxValue.ToIntFloor());
    }

    [Fact]
    public void AddSubtractNegate()
    {
        Assert.Equal(Fixed.FromInt(5), Fixed.FromInt(2) + Fixed.FromInt(3));
        Assert.Equal(3221225472, (Fixed.FromRatio(1, 2) + Fixed.FromRatio(1, 4)).Raw);
        Assert.Equal(Fixed.FromInt(-3), Fixed.FromInt(2) - Fixed.FromInt(5));
        Assert.Equal(Fixed.FromInt(-5), -Fixed.FromInt(5));
        Assert.Throws<OverflowException>(() => Fixed.MaxValue + Fixed.FromRaw(1));
        Assert.Throws<OverflowException>(() => Fixed.MinValue - Fixed.FromRaw(1));
        Assert.Throws<OverflowException>(() => -Fixed.MinValue);
    }

    [Fact]
    public void Multiply_RoundsDownAndChecksOverflow()
    {
        Assert.Equal(9663676416, (Fixed.FromRatio(3, 2) * Fixed.FromRatio(3, 2)).Raw);
        Assert.Equal(-1, (Fixed.FromRaw(-1) * Fixed.FromRaw(1)).Raw);
        Assert.Equal(0, (Fixed.FromRaw(1) * Fixed.FromRaw(1)).Raw);
        Assert.Equal(Fixed.FromInt(-12), Fixed.FromInt(-3) * Fixed.FromInt(4));
        Assert.Equal(Fixed.MaxValue, Fixed.MaxValue * Fixed.One);
        Assert.Equal(Fixed.MinValue, Fixed.MinValue * Fixed.One);
        Assert.Throws<OverflowException>(() => Fixed.FromInt(46341) * Fixed.FromInt(46341));
    }

    [Fact]
    public void Divide_TruncatesTowardZeroAndChecks()
    {
        Assert.Equal(1431655765, (Fixed.FromInt(1) / Fixed.FromInt(3)).Raw);
        Assert.Equal(-1431655765, (Fixed.FromInt(-1) / Fixed.FromInt(3)).Raw);
        Assert.Equal(0, (Fixed.FromRaw(-1) / Fixed.FromInt(2)).Raw);
        Assert.Equal(Fixed.FromRatio(5, 2), Fixed.FromInt(10) / Fixed.FromInt(4));
        Assert.Throws<DivideByZeroException>(() => Fixed.FromInt(1) / Fixed.Zero);
        Assert.Throws<OverflowException>(() => Fixed.MinValue / Fixed.FromInt(-1));
        Assert.Throws<OverflowException>(() => Fixed.FromInt(65536) / Fixed.FromRatio(1, 65536));
    }

    [Fact]
    public void Sqrt_IsFloorOfRoot()
    {
        Assert.Equal(Fixed.Zero, Fixed.Sqrt(Fixed.Zero));
        Assert.Equal(Fixed.One, Fixed.Sqrt(Fixed.One));
        Assert.Equal(Fixed.FromInt(2), Fixed.Sqrt(Fixed.FromInt(4)));
        Assert.Equal(6074000999, Fixed.Sqrt(Fixed.FromInt(2)).Raw);
        Assert.Equal(2147483648, Fixed.Sqrt(Fixed.FromRatio(1, 4)).Raw);
        Assert.Equal(199032864766430, Fixed.Sqrt(Fixed.MaxValue).Raw);
        Assert.Throws<ArgumentOutOfRangeException>(() => Fixed.Sqrt(Fixed.FromRaw(-1)));

        var rng = new SimRandom(7);
        for (var i = 0; i < 1000; i++)
        {
            var x = Fixed.FromRaw((long)(rng.NextU64() >> 1));
            var r = Fixed.Sqrt(x).Raw;
            var n = (UInt128)(ulong)x.Raw << 32;
            Assert.True((UInt128)(ulong)r * (UInt128)(ulong)r <= n);
            Assert.True((UInt128)(ulong)(r + 1) * (UInt128)(ulong)(r + 1) > n);
        }
    }

    [Fact]
    public void AbsMinMaxClamp()
    {
        Assert.Equal(Fixed.FromInt(3), Fixed.Abs(Fixed.FromInt(-3)));
        Assert.Equal(Fixed.FromInt(3), Fixed.Abs(Fixed.FromInt(3)));
        Assert.Throws<OverflowException>(() => Fixed.Abs(Fixed.MinValue));
        Assert.Equal(Fixed.FromInt(1), Fixed.Min(Fixed.FromInt(1), Fixed.FromInt(2)));
        Assert.Equal(Fixed.FromInt(2), Fixed.Max(Fixed.FromInt(1), Fixed.FromInt(2)));
        Assert.Equal(Fixed.FromInt(3), Fixed.Clamp(Fixed.FromInt(5), Fixed.Zero, Fixed.FromInt(3)));
        Assert.Equal(Fixed.Zero, Fixed.Clamp(Fixed.FromInt(-5), Fixed.Zero, Fixed.FromInt(3)));
        Assert.Equal(Fixed.FromInt(2), Fixed.Clamp(Fixed.FromInt(2), Fixed.Zero, Fixed.FromInt(3)));
        Assert.Throws<ArgumentException>(() => Fixed.Clamp(Fixed.One, Fixed.FromInt(3), Fixed.Zero));
    }

    [Fact]
    public void Comparison()
    {
        Assert.True(Fixed.FromInt(1) < Fixed.FromInt(2));
        Assert.True(Fixed.FromInt(2) > Fixed.FromInt(1));
        Assert.True(Fixed.FromInt(2) <= Fixed.FromInt(2));
        Assert.True(Fixed.FromInt(2) >= Fixed.FromInt(2));
        Assert.True(Fixed.FromRaw(-1) < Fixed.Zero);
        Assert.True(Fixed.FromInt(1).CompareTo(Fixed.FromInt(2)) < 0);
        Assert.Equal(0, Fixed.FromInt(2).CompareTo(Fixed.FromInt(2)));
        Assert.Equal(Fixed.FromInt(3), Fixed.FromRatio(6, 2));
        Assert.Equal(Fixed.FromInt(3).GetHashCode(), Fixed.FromRatio(6, 2).GetHashCode());
        var arr = new[] { Fixed.FromInt(3), Fixed.FromInt(-1), Fixed.FromInt(2) };
        Array.Sort(arr);
        Assert.Equal(new[] { Fixed.FromInt(-1), Fixed.FromInt(2), Fixed.FromInt(3) }, arr);
    }

    [Theory]
    [InlineData(0L, "0")]
    [InlineData(4294967296L, "1")]
    [InlineData(-6442450944L, "-1.5")]
    [InlineData(1073741824L, "0.25")]
    [InlineData(1L, "0.00000000023283064365386962890625")]
    [InlineData(-1L, "-0.00000000023283064365386962890625")]
    [InlineData(long.MinValue, "-2147483648")]
    [InlineData(long.MaxValue, "2147483647.99999999976716935634613037109375")]
    public void ToString_IsExactDecimal(long raw, string text)
    {
        Assert.Equal(text, Fixed.FromRaw(raw).ToString());
    }

    private static ulong Mix(ulong acc, long v) => unchecked((acc ^ (ulong)v) * 1099511628211UL);

    private static ulong RunLoop()
    {
        var rng = new SimRandom(20261122);
        var acc = 14695981039346656037UL;
        for (var i = 0; i < 10_000; i++)
        {
            var a = Fixed.FromRaw((long)(rng.NextU64() >> 17) - (1L << 46));
            var b = Fixed.FromRaw((long)(rng.NextU64() >> 17) - (1L << 46));
            acc = Mix(acc, (a + b).Raw);
            acc = Mix(acc, (a - b).Raw);
            acc = Mix(acc, (a * b).Raw);
            if (b != Fixed.Zero)
            {
                acc = Mix(acc, (a / b).Raw);
            }

            acc = Mix(acc, Fixed.Sqrt(Fixed.Abs(a)).Raw);

            Assert.Equal(a + b, b + a);
            Assert.Equal(a * b, b * a);
            Assert.Equal(a, (a + b) - b);
            Assert.Equal(a, a * Fixed.One);
            Assert.Equal(a, a / Fixed.One);
            Assert.Equal(a, a + Fixed.Zero);
            Assert.Equal(a, -(-a));
        }

        return acc;
    }

    [Fact]
    public void Properties_AndGoldenChecksum()
    {
        Assert.Equal(128333051705741941UL, RunLoop());
        Assert.Equal(128333051705741941UL, RunLoop());
    }
}
