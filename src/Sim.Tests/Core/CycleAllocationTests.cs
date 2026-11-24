using System;
using System.Collections.Generic;
using Rts.Sim.Commands;
using Rts.Sim.Core;
using Rts.Sim.Tests.Fixtures;
using Xunit;
using static Rts.Sim.Tests.Fixtures.Refs;

namespace Rts.Sim.Tests.Core;

public sealed class CycleAllocationTests
{
    [Fact]
    public void Step_AndHash_DoNotAllocate()
    {
        // UT-BE-105
        var empty = Array.Empty<Command>();
        var simA = NewSim(S0());
        for (var i = 0; i < 100; i++)
        {
            simA.Step(empty);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            simA.Step(empty);
        }

        var after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0, after - before);

        var simB = NewSim(S0(), new TestIncomeSystem());
        var cmds = new List<Command>
        {
            AddStock(1, Human1, 0, Rts.Sim.Core.Fixed.One),
            AddStock(1, Human1, 1, Rts.Sim.Core.Fixed.One),
            AddStock(1, Human1, 2, Rts.Sim.Core.Fixed.One),
        };
        for (var i = 0; i < 100; i++)
        {
            simB.Step(cmds);
        }

        before = GC.GetAllocatedBytesForCurrentThread();
        SimEvents last = null!;
        for (var i = 0; i < 1000; i++)
        {
            last = simB.Step(cmds);
        }

        after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0, after - before);
        Assert.Equal(3, last.AppliedCount);

        ulong acc = 0;
        for (var i = 0; i < 100; i++)
        {
            acc += simB.ComputeHash();
        }

        before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            acc += simB.ComputeHash() + (ulong)i;
        }

        after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0, after - before);
        Assert.NotEqual(0UL, acc);
    }
}
