using System;
using System.Collections.Generic;
using Rts.Sim.Commands;
using Rts.Sim.Control;
using Rts.Sim.Core;

namespace Rts.Sim.Hosting;

// Contrato do laço de host da AI-SPEC Fase 2 §5.8.4.
public sealed class SimLoop
{
    public const int CommandBufferCapacity = 64;

    private readonly List<Command> _buffer = new(CommandBufferCapacity);

    // Pré-condição não verificável (ControllerSet não expõe o estado): controllers foi criado com
    // simulation.State. Quebrá-la faz Collect ler outro estado; não há uso assim no código.
    public SimLoop(Simulation simulation, ControllerSet controllers, CommandQueue queue)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(controllers);
        ArgumentNullException.ThrowIfNull(queue);
        Simulation = simulation;
        Controllers = controllers;
        Queue = queue;
    }

    public Simulation Simulation { get; }

    public ControllerSet Controllers { get; }

    public CommandQueue Queue { get; }

    // null antes do primeiro Tick; depois, o SimEvents devolvido pelo último Step (mesma instância sempre).
    public SimEvents? LastEvents { get; private set; }

    public SimEvents Tick()
    {
        _buffer.Clear();
        Queue.TakeDue(Simulation.State.Tick, _buffer);
        Controllers.Collect(_buffer);
        var events = Simulation.Step(_buffer);
        LastEvents = events;
        return events;
    }
}
