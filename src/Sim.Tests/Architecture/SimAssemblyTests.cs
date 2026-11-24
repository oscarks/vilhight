using System;
using System.Linq;
using Rts.Sim.Core;
using Xunit;

namespace Rts.Sim.Tests.Architecture;

public sealed class SimAssemblyTests
{
    // UT-BE-01
    [Fact]
    public void SimAssembly_DoesNotReferenceGodot()
    {
        var assembly = typeof(Fixed).Assembly;
        Assert.Equal("Rts.Sim", assembly.GetName().Name);
        var names = assembly.GetReferencedAssemblies().Select(a => a.Name!).ToArray();
        Assert.DoesNotContain(names, n => n.StartsWith("Godot", StringComparison.OrdinalIgnoreCase));
    }
}
