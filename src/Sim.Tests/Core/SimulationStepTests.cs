using System;
using System.Collections.Generic;
using Rts.Sim.Commands;
using Rts.Sim.Core;
using Rts.Sim.State;
using Rts.Sim.Systems;
using Rts.Sim.Tests.Fixtures;
using Xunit;
using static Rts.Sim.Tests.Fixtures.Refs;

namespace Rts.Sim.Tests.Core;

public sealed class SimulationStepTests
{
    private sealed record UnregisteredCommand(CountryId Country, ControllerRef Issuer) : Command(Country, Issuer);

    private static readonly Command[] Empty = Array.Empty<Command>();

    [Fact]
    public void Step_AdvancesTickByOne()
    {
        // UT-BE-80
        var sim = NewSim(S0());
        Assert.Equal(0, sim.State.Tick);
        var e = sim.Step(Empty);
        Assert.Equal(0, e.CompletedTick);
        Assert.Equal(1, sim.State.Tick);
        Assert.Equal(0, e.AppliedCount);
        Assert.Empty(e.Rejections);
        for (var i = 0; i < 9; i++)
        {
            e = sim.Step(Empty);
        }

        Assert.Equal(10, sim.State.Tick);
        Assert.Equal(9, e.CompletedTick);
    }

    [Fact]
    public void Step_AppliesInReceivedOrder()
    {
        // UT-BE-81
        var simA = NewSim(S0());
        var simB = NewSim(S0());
        var eA = simA.Step(new Command[]
        {
            Roll(1, Human1, 0, 1000), Roll(1, Human1, 1, 1000), AddStock(1, Human1, 0, Fixed.FromInt(5)),
        });
        simB.Step(new Command[]
        {
            Roll(1, Human1, 1, 1000), Roll(1, Human1, 0, 1000), AddStock(1, Human1, 0, Fixed.FromInt(5)),
        });
        Assert.Equal(3, eA.AppliedCount);
        var a = simA.State.FindCountry(new CountryId(1))!;
        var b = simB.State.FindCountry(new CountryId(1))!;
        Assert.Equal(Fixed.FromInt(747), a.GetStock(new ResourceId(0)));
        Assert.Equal(Fixed.FromInt(102), a.GetStock(new ResourceId(1)));
        Assert.Equal(Fixed.FromInt(742), b.GetStock(new ResourceId(1)));
        Assert.Equal(Fixed.FromInt(107), b.GetStock(new ResourceId(0)));
        Assert.NotEqual(simA.ComputeHash(), simB.ComputeHash());
    }

    [Fact]
    public void Step_RejectsUnknownCountry()
    {
        // UT-BE-82
        var simA = NewSim(S0());
        var simB = NewSim(S0());
        var e = simA.Step(new Command[] { AddStock(99, Human1, 0, Fixed.One) });
        simB.Step(Empty);
        Assert.Equal(
            new[] { new CommandRejected(0, new CountryId(99), "test.addStock", CommandRejectReason.UnknownCountry) },
            e.Rejections);
        Assert.Equal(0, e.AppliedCount);
        Assert.Equal(simB.ComputeHash(), simA.ComputeHash());
    }

    [Fact]
    public void Step_RejectsNotController()
    {
        // UT-BE-83
        var simA = NewSim(S0());
        var simB = NewSim(S0());
        var e = simA.Step(new Command[] { AddStock(2, Human1, 0, Fixed.One) });
        simB.Step(Empty);
        Assert.Equal(
            new[] { new CommandRejected(0, new CountryId(2), "test.addStock", CommandRejectReason.NotController) },
            e.Rejections);
        Assert.Equal(simB.ComputeHash(), simA.ComputeHash());

        var other = new ControllerRef(ControllerKind.Human, new PlayerId(2));
        e = simA.Step(new Command[] { AddStock(1, other, 0, Fixed.One) });
        Assert.Equal(CommandRejectReason.NotController, e.Rejections[0].Reason);
    }

    [Fact]
    public void Step_RejectsUnknownCommandType()
    {
        // UT-BE-84
        var simA = NewSim(S0());
        var simB = NewSim(S0());
        var e = simA.Step(new Command[] { new UnregisteredCommand(new CountryId(1), Human1) });
        simB.Step(Empty);
        Assert.Equal(
            new[] { new CommandRejected(0, new CountryId(1), "UnregisteredCommand", CommandRejectReason.UnknownCommandType) },
            e.Rejections);
        Assert.Equal(simB.ComputeHash(), simA.ComputeHash());

        e = simA.Step(new Command[] { new UnregisteredCommand(new CountryId(99), Human1) });
        Assert.Equal(CommandRejectReason.UnknownCountry, e.Rejections[0].Reason);
        e = simA.Step(new Command[] { new UnregisteredCommand(new CountryId(2), Human1) });
        Assert.Equal(CommandRejectReason.NotController, e.Rejections[0].Reason);
    }

    [Fact]
    public void Step_HandlerRejection()
    {
        // UT-BE-85
        var state = S0();
        var sim = NewSim(state);
        var e = sim.Step(new Command[]
        {
            AddStock(1, Human1, 5, Fixed.One), Roll(1, Human1, 0, 0), AddStock(1, Human1, 0, Fixed.One),
        });
        Assert.Equal(1, e.AppliedCount);
        Assert.Equal(
            new[]
            {
                new CommandRejected(0, new CountryId(1), "test.addStock", CommandRejectReason.InvalidArguments),
                new CommandRejected(1, new CountryId(1), "test.roll", CommandRejectReason.InvalidArguments),
            },
            e.Rejections);
        Assert.Equal(Fixed.One, state.FindCountry(new CountryId(1))!.GetStock(new ResourceId(0)));
        Assert.Equal(new SimRandom(42).Snapshot(), state.Random.Snapshot());
        Assert.Equal(3, e.AppliedCount + e.Rejections.Count);
    }

    [Fact]
    public void Step_RunsSystemsInOrderAfterCommands()
    {
        // UT-BE-86
        var log = new List<string>();
        var sim = NewSim(S0(), new RecordingSystem("A", log), new RecordingSystem("B", log));
        sim.Step(Empty);
        sim.Step(Empty);
        Assert.Equal(new[] { "A@0", "B@0", "A@1", "B@1" }, log);

        var state = S0();
        var sim2 = NewSim(state, new TestIncomeSystem());
        sim2.Step(new Command[] { AddStock(1, Human1, 0, Fixed.FromInt(2)) });
        var income = Fixed.FromRatio(1, 20);
        Assert.Equal(Fixed.FromInt(2) + income, state.FindCountry(new CountryId(1))!.GetStock(new ResourceId(0)));
        Assert.Equal(income, state.FindCountry(new CountryId(2))!.GetStock(new ResourceId(0)));
        Assert.Equal(income, state.FindCountry(new CountryId(3))!.GetStock(new ResourceId(0)));
    }

    [Fact]
    public void Step_ReusesEvents()
    {
        // UT-BE-87
        var sim = NewSim(S0());
        var first = sim.Step(new Command[] { AddStock(2, Human1, 0, Fixed.One) });
        var second = sim.Step(Empty);
        Assert.Same(first, second);
        Assert.Empty(second.Rejections);
        Assert.Equal(0, second.AppliedCount);
        Assert.Equal(1, second.CompletedTick);
    }

    [Fact]
    public void Step_NullInputsThrowWithoutChangingState()
    {
        // UT-BE-88
        var state = S0();
        var sim = NewSim(state);
        Assert.Throws<ArgumentNullException>(() => sim.Step(null!));
        Assert.Throws<ArgumentException>(() => sim.Step(new Command[] { AddStock(1, Human1, 0, Fixed.One), null! }));
        Assert.Equal(0, state.Tick);
        Assert.Equal(Fixed.Zero, state.FindCountry(new CountryId(1))!.GetStock(new ResourceId(0)));
        Assert.Equal(S0Hash, sim.ComputeHash());
    }

    [Fact]
    public void Simulation_Constructor()
    {
        // UT-BE-89
        var registry = TestCommands.CreateRegistry();
        var none = Array.Empty<ISimSystem>();
        Assert.Throws<ArgumentNullException>(() => new Simulation(null!, none, registry));
        Assert.Throws<ArgumentNullException>(() => new Simulation(S0(), null!, registry));
        Assert.Throws<ArgumentNullException>(() => new Simulation(S0(), none, null!));
        Assert.Throws<ArgumentException>(() => new Simulation(S0(), new ISimSystem[] { null! }, registry));

        var log = new List<string>();
        var systems = new List<ISimSystem> { new RecordingSystem("A", log) };
        var state = S0();
        var sim = new Simulation(state, systems, TestCommands.CreateRegistry());
        systems.Add(new RecordingSystem("B", log));
        sim.Step(Empty);
        Assert.Equal(new[] { "A@0" }, log);
        Assert.Same(state, sim.State);
    }
}
