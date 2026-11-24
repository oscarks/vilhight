using System.Linq;
using Xunit;

namespace Rts.Sim.Tests.Architecture;

public sealed class ForbiddenApiScannerTests
{
    [Theory]
    [InlineData("float x = 1;", "FB-01")]
    [InlineData("var d = 2.0d; double y;", "FB-01")]
    [InlineData("decimal m;", "FB-01")]
    [InlineData("var r = Math.Sqrt(v);", "FB-02")]
    [InlineData("var r = MathF.PI;", "FB-02")]
    [InlineData("var r = new System.Random(1);", "FB-03")]
    [InlineData("var r = new Random(1);", "FB-03")]
    [InlineData("var t = DateTime.Now;", "FB-04")]
    [InlineData("var s = Stopwatch.StartNew();", "FB-04")]
    [InlineData("var t = Environment.TickCount64;", "FB-04")]
    [InlineData("var g = Guid.NewGuid();", "FB-05")]
    [InlineData("Task.Run(() => { });", "FB-06")]
    [InlineData("new Thread(Work);", "FB-06")]
    [InlineData("using Godot;", "FB-07")]
    [InlineData("var p = \"res://data\";", "FB-07")]
    [InlineData("var h = name.GetHashCode();", "FB-08")]
    [InlineData("private static int _count;", "FB-09")]
    [InlineData("static List<int> Cache = new();", "FB-09")]
    public void Scanner_DetectsForbiddenApis(string source, string rule)
    {
        // UT-BE-110
        var violations = ForbiddenApiScanner.ScanSource("x.cs", source);
        Assert.Contains(violations, v => v.Rule == rule && v.Line == 1);
        Assert.Equal(rule, violations.First().Rule);
    }

    [Fact]
    public void Scanner_ReportsLineNumber()
    {
        // UT-BE-110 (linha)
        var violations = ForbiddenApiScanner.ScanSource("x.cs", "int a;\nint b;\nfloat c;");
        var v = Assert.Single(violations);
        Assert.Equal(3, v.Line);
        Assert.Equal("x.cs:3: FB-01 float c;", v.ToString());
    }

    [Theory]
    [InlineData("// double is forbidden")]
    [InlineData("int x = 1; // DateTime.Now")]
    [InlineData("/* float\n double */ int y;")]
    [InlineData("public static readonly Fixed Zero = new(0L);")]
    [InlineData("public const int Max = 8;")]
    [InlineData("public static Fixed operator +(Fixed a, Fixed b) => new(a.Raw + b.Raw);")]
    [InlineData("private static string Num(int v) => v.ToString();")]
    [InlineData("public static class StateHasher")]
    [InlineData("var r = Math.Max(a, b);")]
    [InlineData("var s = \"// not a comment\"; int z;")]
    [InlineData("long doubled = 2;")]
    public void Scanner_IgnoresCommentsAndAllowedForms(string source)
    {
        // UT-BE-111
        Assert.Empty(ForbiddenApiScanner.ScanSource("x.cs", source));
    }

    [Fact]
    public void StripComments_PreservesLineBreaks()
    {
        // UT-BE-111 (adicional)
        Assert.Equal("a     \n     b", ForbiddenApiScanner.StripComments("a /* x\ny */ b"));
    }

    [Fact]
    public void SimSources_HaveNoForbiddenApis()
    {
        // UT-BE-112
        var root = ForbiddenApiScanner.FindRepoRoot();
        var files = ForbiddenApiScanner.ListSourceFiles(root, "src/Sim");
        Assert.Contains("src/Sim/Core/Fixed.cs", files);
        Assert.Contains("src/Sim/Core/Simulation.cs", files);
        Assert.Contains("src/Sim/State/GameState.cs", files);
        Assert.DoesNotContain(files, f => f.Split('/').Any(s => s == "obj" || s == "bin"));
        var violations = ForbiddenApiScanner.ScanDirectory(root, "src/Sim");
        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }
}
