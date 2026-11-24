using System;
using System.Globalization;
using System.Text;

namespace Rts.Sim.Core;

// design: Diretrizes técnicas (determinismo); ADR-002. Q32.32 em long: valor real = Raw / 2^32.
public readonly record struct Fixed(long Raw) : IComparable<Fixed>
{
    public const int FractionBits = 32;

    public static readonly Fixed Zero = new(0L);
    public static readonly Fixed One = new(1L << FractionBits);
    public static readonly Fixed MinValue = new(long.MinValue);
    public static readonly Fixed MaxValue = new(long.MaxValue);

    public static Fixed FromInt(int value) => new((long)value << FractionBits);

    public static Fixed FromRaw(long raw) => new(raw);

    public static Fixed FromRatio(long numerator, long denominator)
    {
        if (denominator == 0)
        {
            throw new DivideByZeroException();
        }

        return new Fixed(checked((long)(((Int128)numerator << FractionBits) / denominator)));
    }

    public int ToIntFloor() => (int)(Raw >> FractionBits);

    public static Fixed operator +(Fixed a, Fixed b) => new(checked(a.Raw + b.Raw));

    public static Fixed operator -(Fixed a, Fixed b) => new(checked(a.Raw - b.Raw));

    public static Fixed operator -(Fixed a) => new(checked(-a.Raw));

    public static Fixed operator *(Fixed a, Fixed b) =>
        new(checked((long)(((Int128)a.Raw * b.Raw) >> FractionBits)));

    public static Fixed operator /(Fixed a, Fixed b)
    {
        if (b.Raw == 0)
        {
            throw new DivideByZeroException();
        }

        return new Fixed(checked((long)(((Int128)a.Raw << FractionBits) / b.Raw)));
    }

    public static bool operator <(Fixed a, Fixed b) => a.Raw < b.Raw;

    public static bool operator >(Fixed a, Fixed b) => a.Raw > b.Raw;

    public static bool operator <=(Fixed a, Fixed b) => a.Raw <= b.Raw;

    public static bool operator >=(Fixed a, Fixed b) => a.Raw >= b.Raw;

    // Piso da raiz, dígito a dígito (sem Math.Sqrt). RN-P1-07.
    public static Fixed Sqrt(Fixed x)
    {
        if (x.Raw < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        var rem = (UInt128)(ulong)x.Raw << FractionBits;
        UInt128 res = 0;
        UInt128 bit = (UInt128)1 << 126;
        while (bit > rem)
        {
            bit >>= 2;
        }

        while (bit != 0)
        {
            if (rem >= res + bit)
            {
                rem -= res + bit;
                res = (res >> 1) + bit;
            }
            else
            {
                res >>= 1;
            }

            bit >>= 2;
        }

        return new Fixed((long)res);
    }

    public static Fixed Abs(Fixed x) => new(checked(x.Raw < 0 ? -x.Raw : x.Raw));

    public static Fixed Min(Fixed a, Fixed b) => a.Raw <= b.Raw ? a : b;

    public static Fixed Max(Fixed a, Fixed b) => a.Raw >= b.Raw ? a : b;

    public static Fixed Clamp(Fixed value, Fixed min, Fixed max)
    {
        if (min.Raw > max.Raw)
        {
            throw new ArgumentException("min must be less than or equal to max");
        }

        return Max(min, Min(value, max));
    }

    public int CompareTo(Fixed other) => Raw.CompareTo(other.Raw);

    // Decimal exato, sem ponto flutuante. RN-P1-09.
    public override string ToString()
    {
        // |Raw| em 128 bits cobre long.MinValue; (ulong)Raw é o complemento de dois.
        var abs = Raw < 0 ? ((UInt128)1 << 64) - (UInt128)(ulong)Raw : (UInt128)(ulong)Raw;
        var integer = (ulong)(abs >> FractionBits);
        var f = (ulong)(abs & 0xFFFFFFFFUL);
        var sb = new StringBuilder();
        if (Raw < 0)
        {
            sb.Append('-');
        }

        sb.Append(integer.ToString(CultureInfo.InvariantCulture));
        if (f != 0)
        {
            sb.Append('.');
            while (f != 0)
            {
                f *= 10;
                sb.Append((char)('0' + (int)(f >> FractionBits)));
                f &= 0xFFFFFFFFUL;
            }
        }

        return sb.ToString();
    }
}
