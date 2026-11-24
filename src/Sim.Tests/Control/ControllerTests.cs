using System;
using System.Collections.Generic;
using Rts.Sim.Commands;
using Rts.Sim.Control;
using Rts.Sim.Core;
using Rts.Sim.State;
using Rts.Sim.Tests.Fixtures;
using Xunit;
using static Rts.Sim.Tests.Fixtures.Refs;

namespace Rts.Sim.Tests.Control;

public sealed class ControllerTests
{
    private static SimEvents RunLoop(Simulation sim, ControllerSet set, List<Command> buffer)
    {
        buffer.Clear();
        set.Collect(buffer);
        return sim.Step(buffer);
    }

    [Fact]
    public void Collect_OnlyAppends()
    {
        // UT-BE-90
        var state = S0();
        var x = AddStock(3, Ai1, 0, Fixed.One);
        var output = new List<Command> { x };
        var human = new HumanController(new PlayerId(1));
        human.Enqueue(AddStock(1, Human1, 0, Fixed.One));
        human.Collect(state, new CountryId(1), 0, output);
        Assert.Equal(2, output.Count);
        Assert.Same(x, output[0]);
    }

    [Fact]
    public void HumanController_FiltersByCountryInEnqueueOrder()
    {
        // UT-BE-91
        var state = S0();
        var h = new HumanController(new PlayerId(1));
        var a = AddStock(1, Human1, 0, Fixed.FromInt(1));
        var b = AddStock(2, Human1, 0, Fixed.FromInt(2));
        var c = AddStock(1, Human1, 0, Fixed.FromInt(3));
        h.Enqueue(a);
        h.Enqueue(b);
        h.Enqueue(c);
        Assert.Equal(Human1, h.Identity);
        Assert.Equal(3, h.PendingCount);

        var output = new List<Command>();
        h.Collect(state, new CountryId(1), 0, output);
        Assert.Equal(2, output.Count);
        Assert.Same(a, output[0]);
        Assert.Same(c, output[1]);
        Assert.Equal(1, h.PendingCount);

        var output2 = new List<Command>();
        h.Collect(state, new CountryId(2), 0, output2);
        Assert.Single(output2);
        Assert.Same(b, output2[0]);
        Assert.Equal(0, h.PendingCount);

        Assert.Throws<ArgumentNullException>(() => h.Enqueue(null!));
        Assert.Throws<ArgumentNullException>(() => h.Collect(null!, new CountryId(1), 0, output));
        Assert.Throws<ArgumentNullException>(() => h.Collect(state, new CountryId(1), 0, null!));
    }

    [Fact]
    public void AiController_IsEmptyStub()
    {
        // UT-BE-92
        var state = S0();
        var ai = new AiController(new PlayerId(1));
        Assert.Equal(Ai1, ai.Identity);
        var output = new List<Command>();
        for (var i = 0; i < 100; i++)
        {
            for (var c = 1; c <= 3; c++)
            {
                ai.Collect(state, new CountryId(c), i, output);
            }
        }

        Assert.Empty(output);
        Assert.Throws<ArgumentNullException>(() => ai.Collect(null!, new CountryId(1), 0, output));
        Assert.Throws<ArgumentNullException>(() => ai.Collect(state, new CountryId(1), 0, null!));
    }

    [Fact]
    public void ControllerSet_CollectsInCountryIdOrder()
    {
        // UT-BE-93
        var state = GameState.Create(SetupFactory.TwoSystemsThreeCountries(), 3);
        var set = new ControllerSet(state);
        var rc = new RecordingController(Human1);
        set.Assign(new CountryId(3), rc);
        set.Assign(new CountryId(1), rc);
        var output = new List<Command>();
        set.Collect(output);
        Assert.Equal(new[] { 1, 3 }, rc.CalledCountries);
        Assert.Equal(2, output.Count);
        Assert.Equal(new CountryId(1), output[0].Country);
        Assert.Null(set.Get(new CountryId(2)));
        Assert.Same(rc, set.Get(new CountryId(1)));
        Assert.Null(set.Get(new CountryId(99)));

        var other = new RecordingController(Human1);
        set.Assign(new CountryId(1), other);
        Assert.Same(other, set.Get(new CountryId(1)));
        Assert.Throws<ArgumentException>(() => set.Assign(new CountryId(99), rc));
        Assert.Throws<ArgumentNullException>(() => set.Assign(new CountryId(1), null!));
        Assert.Throws<ArgumentNullException>(() => set.Collect(null!));
        Assert.Throws<ArgumentNullException>(() => new ControllerSet(null!));

        Assert.Equal(new long[] { 0, 0 }, rc.CalledTicks);
        var sim = NewSim(state);
        sim.Step(Array.Empty<Command>());
        rc.CalledTicks.Clear();
        set.Assign(new CountryId(1), rc);
        set.Collect(output);
        Assert.Equal(new long[] { 1, 1 }, rc.CalledTicks);
    }

    [Fact]
    public void Sim_DoesNotDistinguishHumanFromAi()
    {
        // UT-BE-94
        var cmd = AddStock(1, Human1, 0, Fixed.FromInt(4));
        var buffer = new List<Command>();

        var state1 = S0();
        var sim1 = NewSim(state1);
        var set1 = new ControllerSet(state1);
        var human = new HumanController(new PlayerId(1));
        human.Enqueue(cmd);
        set1.Assign(new CountryId(1), human);
        buffer.Clear();
        set1.Collect(buffer);
        var e1 = sim1.Step(buffer);

        var sim2 = NewSim(S0());
        var e2 = sim2.Step(new Command[] { cmd });

        var state3 = S0();
        var sim3 = NewSim(state3);
        var set3 = new ControllerSet(state3);
        set3.Assign(new CountryId(1), new RecordingController(Human1, _ => Fixed.FromInt(4)));
        buffer.Clear();
        set3.Collect(buffer);
        var e3 = sim3.Step(buffer);

        Assert.Equal(1, e1.AppliedCount);
        Assert.Equal(1, e2.AppliedCount);
        Assert.Equal(1, e3.AppliedCount);
        Assert.Equal(sim1.ComputeHash(), sim2.ComputeHash());
        Assert.Equal(sim2.ComputeHash(), sim3.ComputeHash());
    }

    [Fact]
    public void ControllerSet_EndToEndWithAiCountries()
    {
        // UT-BE-95
        var state = S0();
        var sim = NewSim(state);
        var set = new ControllerSet(state);
        var human = new HumanController(new PlayerId(1));
        set.Assign(new CountryId(1), human);
        set.Assign(new CountryId(2), new AiController(new PlayerId(1)));
        set.Assign(new CountryId(3), new AiController(new PlayerId(1)));
        human.Enqueue(AddStock(1, Human1, 0, Fixed.One));
        human.Enqueue(AddStock(2, Human1, 0, Fixed.One));
        var buffer = new List<Command>();
        var e = RunLoop(sim, set, buffer);
        Assert.Equal(1, e.AppliedCount);
        Assert.Empty(e.Rejections);
        Assert.Equal(1, human.PendingCount);
        e = sim.Step(new Command[] { AddStock(2, Human1, 0, Fixed.One) });
        Assert.Equal(CommandRejectReason.NotController, e.Rejections[0].Reason);
    }
}
