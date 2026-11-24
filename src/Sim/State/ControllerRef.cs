namespace Rts.Sim.State;

// Quem emite comandos para um país: o tipo de controlador e o jogador em nome de quem ele age.
public readonly record struct ControllerRef(ControllerKind Kind, PlayerId Player);
