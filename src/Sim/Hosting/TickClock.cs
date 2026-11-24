using System;
using Rts.Sim.Core;

namespace Rts.Sim.Hosting;

// ADR-003; RN-25 da proposta. Só aritmética de inteiros sobre o tempo que o host informa (nunca lê relógio).
public sealed class TickClock
{
    public const long TickMicros = 50_000;      // 20 ticks/s
    public const int MaxTicksPerAdvance = 5;

    public long AccumulatedMicros { get; private set; }

    public long DroppedMicros { get; private set; }

    public long TotalTicks { get; private set; }

    // Fração do próximo tick já acumulada, em [0, 1).
    public Fixed Alpha => Fixed.FromRatio(AccumulatedMicros, TickMicros);

    // Soma elapsedMicros e devolve quantos ticks o host deve executar agora (0..MaxTicksPerAdvance).
    public int Advance(long elapsedMicros)
    {
        if (elapsedMicros < 0 || elapsedMicros > long.MaxValue - TickMicros)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedMicros));
        }

        var total = AccumulatedMicros + elapsedMicros;
        var due = total / TickMicros;
        int n;
        if (due <= MaxTicksPerAdvance)
        {
            n = (int)due;
            AccumulatedMicros = total - (n * TickMicros);
        }
        else
        {
            // design: RN-25 da proposta — excesso descartado (inclusive a fração) e contado.
            n = MaxTicksPerAdvance;
            DroppedMicros += total - (MaxTicksPerAdvance * TickMicros);
            AccumulatedMicros = 0;
        }

        TotalTicks += n;
        return n;
    }
}
