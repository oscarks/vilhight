using System;
using System.Collections.Generic;
using Rts.Sim.Commands;
using Rts.Sim.Core;
using Rts.Sim.State;
using Rts.Sim.Tests.Fixtures;
using Xunit;

namespace Rts.Sim.Tests.Commands;

public sealed class CommandQueueTests
{
    private static AddStockCommand Cmd(int amount) => Refs.AddStock(1, Refs.Human1, 0, Fixed.FromInt(amount));

    [Fact]
    public void Queue_DeliversAtRequestedTickInStableOrder()
    {
        // UT-BE-77
        var q = new CommandQueue();
        var a = Cmd(1);
        var b = Cmd(2);
        var c = Cmd(3);
        var d = Cmd(4);
        q.Enqueue(2, a);
        q.Enqueue(1, b);
        q.Enqueue(2, c);
        q.Enqueue(1, d);
        Assert.Equal(-1, q.LastTakenTick);
        Assert.Equal(4, q.Count);

        var output = new List<Command>();
        q.TakeDue(0, output);
        Assert.Empty(output);
        q.TakeDue(1, output);
        Assert.Equal(2, output.Count);
        Assert.Same(b, output[0]);
        Assert.Same(d, output[1]);

        q.TakeDue(2, output);
        Assert.Equal(4, output.Count);
        Assert.Same(b, output[0]);
        Assert.Same(d, output[1]);
        Assert.Same(a, output[2]);
        Assert.Same(c, output[3]);
        Assert.Equal(0, q.Count);
        Assert.Equal(2, q.LastTakenTick);
    }

    [Fact]
    public void Queue_TakeDueCatchesUpSkippedTicks()
    {
        // UT-BE-78
        var q = new CommandQueue();
        var a = Cmd(1);
        var b = Cmd(2);
        var c = Cmd(3);
        q.Enqueue(3, a);
        q.Enqueue(5, b);
        q.Enqueue(4, c);
        var output = new List<Command>();
        q.TakeDue(4, output);
        Assert.Equal(2, output.Count);
        Assert.Same(a, output[0]);
        Assert.Same(c, output[1]);
        Assert.Equal(1, q.Count);
    }

    [Fact]
    public void Queue_Guards()
    {
        // UT-BE-79
        var q = new CommandQueue();
        var a = Cmd(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => q.Enqueue(-1, a));
        var output = new List<Command>();
        q.TakeDue(5, output);
        Assert.Throws<ArgumentOutOfRangeException>(() => q.Enqueue(5, a));
        Assert.Throws<ArgumentOutOfRangeException>(() => q.TakeDue(5, output));
        Assert.Throws<ArgumentOutOfRangeException>(() => q.TakeDue(4, output));
        q.Enqueue(6, a);
        Assert.Throws<ArgumentNullException>(() => q.Enqueue(7, null!));
        Assert.Throws<ArgumentNullException>(() => q.TakeDue(6, null!));
    }

    [Fact]
    public void Queue_IntegrationWithStep()
    {
        // UT-BE-79 (integração com Step)
        var state = Refs.S0();
        var sim = Refs.NewSim(state);
        var q = new CommandQueue();
        q.Enqueue(5, Cmd(3));
        var buffer = new List<Command>();
        var country = state.FindCountry(new CountryId(1))!;
        for (var i = 0; i < 5; i++)
        {
            buffer.Clear();
            q.TakeDue(state.Tick, buffer);
            sim.Step(buffer);
        }

        Assert.Equal(5, state.Tick);
        Assert.Equal(Fixed.Zero, country.GetStock(new ResourceId(0)));
        buffer.Clear();
        q.TakeDue(state.Tick, buffer);
        sim.Step(buffer);
        Assert.Equal(Fixed.FromInt(3), country.GetStock(new ResourceId(0)));
    }
}
