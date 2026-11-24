namespace Rts.Sim.Commands;

public enum CommandRejectReason : byte
{
    None = 0,               // não é rejeição: handler aplicou
    UnknownCountry = 1,
    NotController = 2,
    UnknownCommandType = 3,
    InvalidArguments = 4,   // devolvido por handler (argumento fora do domínio)
}
