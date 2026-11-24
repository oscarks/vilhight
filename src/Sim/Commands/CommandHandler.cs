using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Commands;

// Devolve None se aplicou; qualquer outro valor rejeita o comando.
// Contrato: o handler NÃO altera o estado quando devolve um valor diferente de None.
public delegate CommandRejectReason CommandHandler<in T>(GameState state, SimContext context, T command)
    where T : Command;
