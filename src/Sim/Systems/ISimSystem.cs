using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Systems;

// CLAUDE.md §6: sistemas são funções do estado; sem dependências ocultas.
public interface ISimSystem
{
    void Tick(GameState state, SimContext context);
}
