using System;
using Rts.Sim.State;

namespace Rts.Sim.Core;

// ADR-002; DD-03 da proposta: FNV-1a 64, não criptográfico, só detecta divergência.
// Bytes em little-endian por deslocamento (nunca BitConverter). Ordem canônica: AI-SPEC Fase 2 RN-P2-51.
public static class StateHasher
{
    public const ulong OffsetBasis = 14695981039346656037UL;
    public const ulong Prime = 1099511628211UL;

    public static ulong Compute(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var h = OffsetBasis;
        h = MixInt64(h, state.Tick);
        h = MixUInt64(h, state.Seed);
        var (s0, s1, s2, s3) = state.Random.Snapshot();
        h = MixUInt64(h, s0);
        h = MixUInt64(h, s1);
        h = MixUInt64(h, s2);
        h = MixUInt64(h, s3);
        h = MixInt32(h, state.ResourceCount);

        h = MixInt32(h, state.Players.Count);
        for (var i = 0; i < state.Players.Count; i++)
        {
            var p = state.Players[i];
            h = MixInt32(h, p.Id.Value);
            h = MixString(h, p.Name);
        }

        var systems = state.Universe.StarSystems;
        h = MixInt32(h, systems.Count);
        for (var i = 0; i < systems.Count; i++)
        {
            var s = systems[i];
            h = MixInt32(h, s.Id.Value);
            h = MixInt32(h, s.Owner.Value);
            h = MixInt32(h, s.Worlds.Count);
            for (var j = 0; j < s.Worlds.Count; j++)
            {
                var w = s.Worlds[j];
                h = MixInt32(h, w.Id.Value);
                h = MixInt32(h, w.Countries.Count);
                for (var k = 0; k < w.Countries.Count; k++)
                {
                    var c = w.Countries[k];
                    h = MixInt32(h, c.Id.Value);
                    h = MixString(h, c.Name);
                    h = MixByte(h, (byte)c.Controller.Kind);
                    h = MixInt32(h, c.Controller.Player.Value);
                    var stock = c.Stock;
                    h = MixInt32(h, stock.Length);
                    foreach (var v in stock)
                    {
                        h = MixInt64(h, v.Raw);
                    }
                }
            }
        }

        return h;
    }

    private static ulong MixByte(ulong h, byte b) => unchecked((h ^ b) * Prime);

    private static ulong MixInt32(ulong h, int v)
    {
        var u = unchecked((uint)v);
        for (var i = 0; i < 4; i++)
        {
            h = MixByte(h, (byte)(u >> (8 * i)));
        }

        return h;
    }

    private static ulong MixInt64(ulong h, long v) => MixUInt64(h, unchecked((ulong)v));

    private static ulong MixUInt64(ulong h, ulong v)
    {
        for (var i = 0; i < 8; i++)
        {
            h = MixByte(h, (byte)(v >> (8 * i)));
        }

        return h;
    }

    private static ulong MixString(ulong h, string s)
    {
        h = MixInt32(h, s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            var ch = s[i];
            h = MixByte(h, (byte)ch);
            h = MixByte(h, (byte)(ch >> 8));
        }

        return h;
    }
}
