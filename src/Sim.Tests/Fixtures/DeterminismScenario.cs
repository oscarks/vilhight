using System;
using System.Collections.Generic;
using Rts.Sim.Commands;
using Rts.Sim.Core;
using Rts.Sim.State;
using Rts.Sim.Systems;

namespace Rts.Sim.Tests.Fixtures;

public static class DeterminismScenario
{
    public const int Ticks = 1000;
    public const ulong StateSeed = 42;
    public const ulong ScriptSeed = 777;
    public const int ResourceCount = 5;

    // Gera a lista de comandos de cada tick (índice = tick). Mesma entrada => mesma saída.
    public static List<Command>[] BuildScript()
    {
        var script = new SimRandom(ScriptSeed);
        var result = new List<Command>[Ticks];
        for (var t = 0; t < Ticks; t++)
        {
            var list = new List<Command>();
            var n = script.NextInt(4);
            for (var i = 0; i < n; i++)
            {
                var c = 1 + script.NextInt(3);
                var kind = script.NextInt(2);
                var r = script.NextInt(5);
                if (kind == 0)
                {
                    list.Add(new AddStockCommand(
                        new CountryId(c), IssuerOf(c), new ResourceId(r), Fixed.FromInt(script.NextInt(10))));
                }
                else
                {
                    list.Add(new RollCommand(
                        new CountryId(c), IssuerOf(c), new ResourceId(r), 1 + script.NextInt(6)));
                }
            }

            if (t % 50 == 49)
            {
                list.Add(new AddStockCommand(
                    new CountryId(2),
                    new ControllerRef(ControllerKind.Human, new PlayerId(1)),
                    new ResourceId(0),
                    Fixed.FromInt(1000)));
            }

            result[t] = list;
        }

        return result;
    }

    // Hashes[0] depois de beforeFirstStep e antes do primeiro Step; Hashes[t] depois do t-ésimo Step.
    public static ScenarioResult Run(
        List<Command>[] script, ulong stateSeed = StateSeed, Action<GameState>? beforeFirstStep = null)
    {
        var state = GameState.Create(SetupFactory.OneSystemThreeCountries(stateSeed), ResourceCount);
        beforeFirstStep?.Invoke(state);
        var sim = new Simulation(state, new ISimSystem[] { new TestIncomeSystem() }, TestCommands.CreateRegistry());
        var hashes = new ulong[script.Length + 1];
        hashes[0] = sim.ComputeHash();
        var applied = 0;
        var rejected = 0;
        for (var t = 0; t < script.Length; t++)
        {
            var events = sim.Step(script[t]);
            applied += events.AppliedCount;
            rejected += events.Rejections.Count;
            hashes[t + 1] = sim.ComputeHash();
        }

        return new ScenarioResult(hashes, applied, rejected, state);
    }

    private static ControllerRef IssuerOf(int country) => country switch
    {
        1 => new ControllerRef(ControllerKind.Human, new PlayerId(1)),
        _ => new ControllerRef(ControllerKind.Ai, new PlayerId(1)),
    };
}

public sealed record ScenarioResult(ulong[] Hashes, int Applied, int Rejected, GameState FinalState);
