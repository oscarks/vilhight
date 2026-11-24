using System.Collections.Generic;
using System.IO;
using Rts.Sim.Data;
using Xunit;

namespace Rts.Sim.Tests.Architecture;

public sealed class ContentIdScannerTests
{
    [Fact]
    public void ContentIdScanner_FindsLiterals()
    {
        // UT-BE-141
        Assert.Equal(new[] { new ContentIdScanner.Literal(1, "wood") }, ContentIdScanner.FindStringLiterals("var a = \"wood\";"));
        Assert.Equal(
            new[] { new ContentIdScanner.Literal(1, "a\"b"), new ContentIdScanner.Literal(1, "d") },
            ContentIdScanner.FindStringLiterals("x(\"a\\\"b\", 'c', \"d\");"));
        Assert.Equal(new[] { new ContentIdScanner.Literal(1, "n {v}") }, ContentIdScanner.FindStringLiterals("var s = $\"n {v}\";"));
        Assert.Equal(new[] { new ContentIdScanner.Literal(2, "x") }, ContentIdScanner.FindStringLiterals("int a;\nvar b = \"x\";"));
        Assert.Empty(ContentIdScanner.FindStringLiterals(ForbiddenApiScanner.StripComments("// \"ore\"")));
        Assert.True(ContentIdScanner.Matches("ore", "ore"));
        Assert.True(ContentIdScanner.Matches("unknown ore", "ore"));
        Assert.False(ContentIdScanner.Matches("before", "ore"));
        Assert.False(ContentIdScanner.Matches("ore_x", "ore"));
        Assert.False(ContentIdScanner.Matches("core", "ore"));
    }

    [Fact]
    public void SimSources_HaveNoContentIdLiterals()
    {
        // UT-BE-142
        var root = ForbiddenApiScanner.FindRepoRoot();
        var data = DataLoader.Load(Path.Combine(root, "game", "data"));
        var keys = ContentIdScanner.ContentKeys(data);
        Assert.Equal(
            new[]
            {
                "food", "knowledge", "metal", "mine", "ore", "smelt_metal", "smelter",
                "tech_agriculture", "tech_forestry", "tech_mining", "tech_smelting", "wood",
            },
            new List<string>(keys));
        Assert.Equal(12, keys.Count);
        var violations = ContentIdScanner.Scan(root, "src/Sim", keys);
        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }
}
