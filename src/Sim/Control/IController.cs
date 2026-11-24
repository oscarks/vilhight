using System.Collections.Generic;
using Rts.Sim.Commands;
using Rts.Sim.State;

namespace Rts.Sim.Control;

// ADR-004, RF-05: humano e IA usam a mesma API; a Sim não os distingue.
public interface IController
{
    ControllerRef Identity { get; }

    // Acrescenta a output os comandos do país para o tick. Não limpa output. Não altera o estado.
    void Collect(GameState state, CountryId country, long tick, List<Command> output);
}
