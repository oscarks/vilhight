using System;
using System.Collections.Generic;
using System.IO;
using Rts.Sim.Commands;
using Rts.Sim.Control;
using Rts.Sim.Core;
using Rts.Sim.Data;
using Rts.Sim.Hosting;
using Rts.Sim.State;
using Rts.Sim.Systems;
using Rts.Sim.Tests.Architecture;
using Rts.Sim.Tests.Fixtures;
using Xunit;
using static Rts.Sim.Tests.Fixtures.Refs;

namespace Rts.Sim.Tests.Hosting;

public sealed class SimLoopTests
{
    private static (Simulation Sim, ControllerSet Set, HumanController Human, CommandQueue Queue, SimLoop Loop) Build()
    {
        var sim = NewSim(S0());
        var set = new ControllerSet(sim.State);
        var human = new HumanController(new PlayerId(1));
        set.Assign(new CountryId(1), human);
        set.Assign(new CountryId(2), new AiController(new PlayerId(1)));
        set.Assign(new CountryId(3), new AiController(new PlayerId(1)));
        var q = new CommandQueue();
        return (sim, set, human, q, new SimLoop(sim, set, q));
    }

    [Fact]
    public void Ctor_RejectsNull()
    {
        // UT-BE-157
        var sim = NewSim(S0());
        var cs = new ControllerSet(sim.State);
        var q = new CommandQueue();
        Assert.Equal("simulation", Assert.Throws<ArgumentNullException>(() => new SimLoop(null!, cs, q)).ParamName);
        Assert.Equal("controllers", Assert.Throws<ArgumentNullException>(() => new SimLoop(sim, null!, q)).ParamName);
        Assert.Equal("queue", Assert.Throws<ArgumentNullException>(() => new SimLoop(sim, cs, null!)).ParamName);
        var loop = new SimLoop(sim, cs, q);
        Assert.Null(loop.LastEvents);
        Assert.Same(sim, loop.Simulation);
        Assert.Same(cs, loop.Controllers);
        Assert.Same(q, loop.Queue);
    }

    [Fact]
    public void Tick_QueueCommandsComeBeforeControllerCommands()
    {
        // UT-BE-158
        var (_, _, human, q, loop) = Build();
        q.Enqueue(0, AddStock(2, Human1, 0, Fixed.One));
        human.Enqueue(AddStock(1, Ai1, 0, Fixed.One));
        var ev = loop.Tick();
        Assert.Equal(2, ev.Rejections.Count);
        Assert.Equal(0, ev.AppliedCount);
        Assert.Equal(0, ev.Rejections[0].CommandIndex);
        Assert.Equal(new CountryId(2), ev.Rejections[0].Country);
        Assert.Equal(CommandRejectReason.NotController, ev.Rejections[0].Reason);
        Assert.Equal(1, ev.Rejections[1].CommandIndex);
        Assert.Equal(new CountryId(1), ev.Rejections[1].Country);
        Assert.Equal(CommandRejectReason.NotController, ev.Rejections[1].Reason);
        Assert.Equal(0, human.PendingCount);
        Assert.Equal(0, q.Count);
    }

    [Fact]
    public void Tick_AdvancesStateAndReturnsEvents()
    {
        // UT-BE-159
        var (sim, _, _, q, loop) = Build();
        var e1 = loop.Tick();
        var e2 = loop.Tick();
        Assert.Same(e1, e2);
        Assert.Same(loop.LastEvents, e2);
        Assert.Equal(1, e2.CompletedTick);
        Assert.Equal(2, sim.State.Tick);
        Assert.Equal(1, q.LastTakenTick);
        Assert.Equal(0, e2.AppliedCount);
        Assert.Empty(e2.Rejections);
    }

    [Fact]
    public void Tick_HonorsScheduledTick()
    {
        // UT-BE-160
        var (sim, _, _, q, loop) = Build();
        q.Enqueue(2, AddStock(1, Human1, 3, Fixed.FromInt(7)));
        var applied = new int[3];
        for (var i = 0; i < 3; i++)
        {
            applied[i] = loop.Tick().AppliedCount;
        }

        Assert.Equal(new[] { 0, 0, 1 }, applied);
        Assert.Equal(Fixed.FromInt(7), sim.State.FindCountry(new CountryId(1))!.GetStock(new ResourceId(3)));
    }

    [Fact]
    public void Tick_DoesNotAllocate()
    {
        // UT-BE-161
        var (sim, _, _, _, loop) = Build();
        for (var i = 0; i < 100; i++)
        {
            loop.Tick();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            loop.Tick();
        }

        var after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0, after - before);
        Assert.Equal(1100, sim.State.Tick);
    }

    [Fact]
    public void DemoSetupEquivalent_ReferenceHashes()
    {
        // E2E-BE-20
        var root = ForbiddenApiScanner.FindRepoRoot();
        var data = DataLoader.Load(Path.Combine(root, "game", "data"));
        var state = GameState.Create(SetupFactory.OneSystemThreeCountries(42), data);
        var sim = new Simulation(state, Array.Empty<ISimSystem>(), new CommandRegistry());
        var cs = new ControllerSet(state);
        var human = new HumanController(new PlayerId(1));
        foreach (var country in state.Countries)
        {
            cs.Assign(
                country.Id,
                country.Controller.Kind == ControllerKind.Human ? human : new AiController(country.Controller.Player));
        }

        var loop = new SimLoop(sim, cs, new CommandQueue());
        Assert.Equal(S0Hash, sim.ComputeHash());
        var rejections = 0;
        void RunTo(long tick)
        {
            while (sim.State.Tick < tick)
            {
                rejections += loop.Tick().Rejections.Count;
            }
        }

        RunTo(1);
        Assert.Equal(5242866997788935815UL, sim.ComputeHash());
        RunTo(100);
        Assert.Equal(8913928992633490408UL, sim.ComputeHash());
        RunTo(1200);
        Assert.Equal(14084532596178668976UL, sim.ComputeHash());
        Assert.Equal(0, rejections);
    }
}
