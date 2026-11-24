using System.Collections.Generic;
using System.Reflection;
using Rts.Sim.Commands;
using Rts.Sim.Core;
using Xunit;

namespace Rts.Sim.Tests.Architecture;

public sealed class CycleEncapsulationTests
{
    [Fact]
    public void Cycle_IsReadOnlyForPresentation()
    {
        // UT-BE-113
        Assert.Null(typeof(SimEvents).GetMethod("Begin"));
        Assert.Null(typeof(SimEvents).GetMethod("AddApplied"));
        Assert.Null(typeof(SimEvents).GetMethod("AddRejection"));
        Assert.Empty(typeof(SimEvents).GetConstructors());
        Assert.Empty(typeof(SimContext).GetConstructors());
        Assert.False(typeof(SimContext).GetProperty("Tick")!.SetMethod!.IsPublic);
        Assert.Null(typeof(CommandRegistry).GetMethod("TryGetEntry"));
        Assert.NotNull(typeof(CommandRegistry).GetMethod("TryGetEntry", BindingFlags.NonPublic | BindingFlags.Instance));

        var sim = new Simulation(
            Fixtures.Refs.S0(),
            System.Array.Empty<Rts.Sim.Systems.ISimSystem>(),
            Fixtures.TestCommands.CreateRegistry());
        var events = sim.Step(System.Array.Empty<Command>());
        Assert.False(events.Rejections is List<CommandRejected>);
        Assert.Throws<System.NotSupportedException>(() => ((IList<CommandRejected>)events.Rejections).Add(default));
    }
}
