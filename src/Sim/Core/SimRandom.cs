using System;

namespace Rts.Sim.Core;

// ADR-002; DD-02 da proposta: xoshiro256** semeado por SplitMix64.
public sealed class SimRandom
{
    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;

    public SimRandom(ulong seed)
    {
        unchecked
        {
            var z = seed;
            _s0 = SplitMix64(ref z);
            _s1 = SplitMix64(ref z);
            _s2 = SplitMix64(ref z);
            _s3 = SplitMix64(ref z);
        }
    }

    public ulong NextU64()
    {
        unchecked
        {
            var result = Rotl(_s1 * 5, 7) * 9;
            var t = _s1 << 17;
            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = Rotl(_s3, 45);
            return result;
        }
    }

    // Sem viés, por rejeição. Não consome valor quando o argumento é inválido.
    public int NextInt(int exclusiveMax)
    {
        if (exclusiveMax <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
        }

        unchecked
        {
            var bound = (ulong)exclusiveMax;
            var threshold = (0UL - bound) % bound;
            while (true)
            {
                var x = NextU64();
                if (x >= threshold)
                {
                    return (int)(x % bound);
                }
            }
        }
    }

    // Fração em [0, 1): 32 bits altos como parte fracionária.
    public Fixed NextFixed() => new((long)(NextU64() >> 32));

    public (ulong S0, ulong S1, ulong S2, ulong S3) Snapshot() => (_s0, _s1, _s2, _s3);

    private static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));

    private static ulong SplitMix64(ref ulong z)
    {
        unchecked
        {
            z += 0x9E3779B97F4A7C15UL;
            var t = z;
            t = (t ^ (t >> 30)) * 0xBF58476D1CE4E5B9UL;
            t = (t ^ (t >> 27)) * 0x94D049BB133111EBUL;
            return t ^ (t >> 31);
        }
    }
}
