using System;
using Rts.Sim.Commands;
using Rts.Sim.Core;
using Rts.Sim.State;
using Rts.Sim.Tests.Fixtures;
using Xunit;

namespace Rts.Sim.Tests.Commands;

public sealed class CommandRegistryTests
{
    private static CommandRejectReason Noop(GameState s, SimContext c, AddStockCommand cmd) =>
        CommandRejectReason.None;

    private static CommandRejectReason NoopRoll(GameState s, SimContext c, RollCommand cmd) =>
        CommandRejectReason.None;

    [Fact]
    public void Register_ValidatesNamesAndUniqueness()
    {
        // UT-BE-70
        Assert.Equal(2, TestCommands.CreateRegistry().Count);

        foreach (var bad in new[] { string.Empty, "Test", "1a", "a..b", "a.", "a-b", new string('a', 65) })
        {
            var r = new CommandRegistry();
            Assert.Throws<ArgumentException>(() => r.Register<AddStockCommand>(bad, Noop));
        }

        foreach (var good in new[] { "a", "test.addStock", "x9.y8Z" })
        {
            var r = new CommandRegistry();
            r.Register<AddStockCommand>(good, Noop);
            Assert.Equal(1, r.Count);
        }

        var reg = TestCommands.CreateRegistry();
        Assert.Throws<InvalidOperationException>(() => reg.Register<RollCommand>(TestCommands.AddStock, NoopRoll));
        Assert.Throws<InvalidOperationException>(() => reg.Register<AddStockCommand>("other.name", Noop));
        Assert.Throws<ArgumentNullException>(() => new CommandRegistry().Register<AddStockCommand>(null!, Noop));
        Assert.Throws<ArgumentNullException>(() => new CommandRegistry().Register<AddStockCommand>("a", null!));
        Assert.Throws<ArgumentException>(() => new CommandRegistry().Register<Command>("a", (s, c, cmd) => CommandRejectReason.None));
    }

    [Fact]
    public void Registry_FreezeRules()
    {
        // UT-BE-71
        var registry = TestCommands.CreateRegistry();
        Assert.False(registry.IsFrozen);
        registry.Freeze();
        Assert.True(registry.IsFrozen);
        registry.Freeze();
        Assert.Throws<InvalidOperationException>(() => registry.Register<AddStockCommand>("zzz", Noop));
        Assert.Equal(2, registry.Count);

        var r1 = TestCommands.CreateRegistry();
        _ = new Simulation(Refs.S0(), Array.Empty<Rts.Sim.Systems.ISimSystem>(), r1);
        Assert.True(r1.IsFrozen);

        var r2 = TestCommands.CreateRegistry();
        _ = new CommandCodec(r2);
        Assert.True(r2.IsFrozen);
    }

    [Fact]
    public void Registry_Lookups()
    {
        // UT-BE-72
        var registry = TestCommands.CreateRegistry();
        Assert.True(registry.TryGetTypeName(typeof(AddStockCommand), out var n));
        Assert.Equal("test.addStock", n);
        Assert.True(registry.TryGetCommandType("test.roll", out var t));
        Assert.Equal(typeof(RollCommand), t);
        Assert.False(registry.TryGetTypeName(typeof(string), out _));
        Assert.False(registry.TryGetCommandType("nope", out _));
    }
}
