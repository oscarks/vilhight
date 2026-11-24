using Rts.Sim.State;

namespace Rts.Sim.Commands;

// CommandIndex: posição do comando na lista recebida por Step.
// CommandType: nome registrado; para tipo não registrado, command.GetType().Name.
public readonly record struct CommandRejected(
    int CommandIndex,
    CountryId Country,
    string CommandType,
    CommandRejectReason Reason);
