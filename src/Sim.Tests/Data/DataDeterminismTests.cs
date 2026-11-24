using System.IO;
using Rts.Sim.Data;
using Rts.Sim.Tests.Architecture;
using Xunit;
using static Rts.Sim.Tests.Fixtures.DataFixtures;

namespace Rts.Sim.Tests.Data;

public sealed class DataDeterminismTests
{
    [Fact]
    public void Compile_IsDeterministic()
    {
        // UT-BE-143
        Assert.Equal(Dump(DataLoader.Parse(Base())), Dump(DataLoader.Parse(Base())));
        var raw = Raw(
            Resources(
                """{"id":"r_a","category":"basic","storage":"stock"}""",
                """{"id":"r_a","category":"basic","storage":"stock"}"""),
            Techs("""{"id":"t_a","tier":1,"cost":{"r_a":1},"researchTicks":1,"requires":["t_zz"],"effects":[{"type":"teleport"}]}"""));
        Assert.Equal(DataValidator.Validate(raw), DataValidator.Validate(raw));
        var dir = Path.Combine(ForbiddenApiScanner.FindRepoRoot(), "game", "data");
        Assert.Equal(Dump(DataLoader.Load(dir)), Dump(DataLoader.Load(dir)));
    }
}
