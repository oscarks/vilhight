using Rts.Sim.Core;

namespace Rts.Game;

// ADR-001 / P-06: a única conversão Fixed → float do projeto.
public static class FixedConvert
{
    public static float ToFloat(Fixed value) => (float)(value.Raw / 4294967296.0); // 2^32
}
