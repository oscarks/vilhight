using System;
using System.Collections.Generic;
using System.Globalization;
using Rts.Sim.Commands;
using Rts.Sim.State;
using Rts.Sim.Systems;

namespace Rts.Sim.Core;

// ADR-003: Step = 1 tick. Só aqui o estado muda (comandos e sistemas); ADR-004: rejeição é evento.
public sealed class Simulation
{
    private readonly ISimSystem[] _systems;
    private readonly CommandRegistry _registry;
    private readonly SimContext _context;
    private readonly SimEvents _events;

    public Simulation(GameState state, IReadOnlyList<ISimSystem> systems, CommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(systems);
        ArgumentNullException.ThrowIfNull(registry);
        var copy = new ISimSystem[systems.Count];
        for (var i = 0; i < copy.Length; i++)
        {
            copy[i] = systems[i]
                ?? throw new ArgumentException(
                    "systems[" + i.ToString(CultureInfo.InvariantCulture) + "] is null", nameof(systems));
        }

        registry.Freeze();
        State = state;
        _systems = copy;
        _registry = registry;
        _events = new SimEvents();
        _context = new SimContext(_events);
    }

    public GameState State { get; }

    public SimEvents Step(IReadOnlyList<Command> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        for (var i = 0; i < commands.Count; i++)
        {
            if (commands[i] is null)
            {
                throw new ArgumentException(
                    "commands[" + i.ToString(CultureInfo.InvariantCulture) + "] is null", nameof(commands));
            }
        }

        var tick = State.Tick;
        _context.Tick = tick;
        _events.Begin(tick);

        for (var i = 0; i < commands.Count; i++)
        {
            var cmd = commands[i];
            var known = _registry.TryGetEntry(cmd.GetType(), out var entry);
            var reason = CommandRejectReason.None;
            var country = State.FindCountry(cmd.Country);
            if (country is null)
            {
                reason = CommandRejectReason.UnknownCountry;
            }
            else if (country.Controller != cmd.Issuer)
            {
                reason = CommandRejectReason.NotController;
            }
            else if (!known)
            {
                reason = CommandRejectReason.UnknownCommandType;
            }
            else
            {
                reason = entry.Invoke(State, _context, cmd);
            }

            if (reason == CommandRejectReason.None)
            {
                _events.AddApplied();
            }
            else
            {
                _events.AddRejection(new CommandRejected(
                    i, cmd.Country, known ? entry.TypeName : cmd.GetType().Name, reason));
            }
        }

        for (var k = 0; k < _systems.Length; k++)
        {
            _systems[k].Tick(State, _context);
        }

        State.Tick = tick + 1;
        return _events;
    }

    public ulong ComputeHash() => StateHasher.Compute(State);
}
