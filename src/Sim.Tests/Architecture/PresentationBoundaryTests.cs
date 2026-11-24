using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Rts.Sim.Core;
using Rts.Sim.Data;
using Xunit;

namespace Rts.Sim.Tests.Architecture;

public sealed class PresentationBoundaryTests
{
    [Fact]
    public void SimInternals_AreVisibleOnlyToTests()
    {
        // UT-BE-162
        var attrs = typeof(Simulation).Assembly.GetCustomAttributes<InternalsVisibleToAttribute>();
        Assert.Equal(new[] { "Rts.Sim.Tests" }, attrs.Select(a => a.AssemblyName).ToArray());
    }

    [Fact]
    public void GameScripts_RespectPresentationBoundary()
    {
        // UT-BE-163
        var root = ForbiddenApiScanner.FindRepoRoot();
        var keys = ContentIdScanner.ContentKeys(DataLoader.Load(Path.Combine(root, "game", "data")));
        var violations = ContentIdScanner.Scan(root, "game/scripts", keys);
        Assert.True(violations.Count == 0, string.Join("\n", violations));

        var files = ForbiddenApiScanner.ListSourceFiles(root, "game/scripts");
        foreach (var expected in new[] { "SimHost", "CameraRig", "IsoGrid", "HudTick", "ResDataReader" })
        {
            Assert.Contains("game/scripts/" + expected + ".cs", files);
        }

        foreach (var rel in files)
        {
            var text = ForbiddenApiScanner.StripComments(File.ReadAllText(Path.Combine(root, rel)));
            if (rel.EndsWith("/CameraRig.cs", StringComparison.Ordinal) || rel.EndsWith("/IsoGrid.cs", StringComparison.Ordinal))
            {
                Assert.DoesNotContain("Rts.Sim", text);
                Assert.DoesNotContain("SimHost", text);
            }

            Assert.DoesNotContain("GlobalizePath", text);
            Assert.DoesNotContain("\"resources.json\"", text);
            Assert.DoesNotContain("\"techs.json\"", text);
        }

        var reader = File.ReadAllText(Path.Combine(root, "game/scripts/ResDataReader.cs"));
        Assert.Contains("DataFiles.Resources", reader);
        Assert.Contains("DataFiles.Techs", reader);
        Assert.Contains("DataLimits.MaxFileBytes", reader);
    }

    [Fact]
    public void GameProject_IsWiredAsSpecified()
    {
        // UT-BE-164
        var root = ForbiddenApiScanner.FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(root, "game", "Rts.Game.csproj"));
        var sln = File.ReadAllText(Path.Combine(root, "Rts.sln"));
        Assert.Contains("Sdk=\"Godot.NET.Sdk/4.7.2\"", csproj);
        Assert.Contains("<ProjectReference Include=\"../src/Sim/Rts.Sim.csproj\" />", csproj);
        Assert.Contains("<Nullable>enable</Nullable>", csproj);
        Assert.Contains("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", csproj);
        Assert.Contains("<TargetFramework>net8.0</TargetFramework>", csproj);
        Assert.Contains("game\\Rts.Game.csproj", sln);
        var gameSln = File.ReadAllText(Path.Combine(root, "game", "Rts.Game.sln"));
        Assert.Contains("Rts.Game.csproj", gameSln);
        Assert.False(File.Exists(Path.Combine(root, "Directory.Build.props")));
        Assert.False(File.Exists(Path.Combine(root, "Directory.Packages.props")));
        Assert.False(File.Exists(Path.Combine(root, "game", "Directory.Build.props")));
        Assert.DoesNotContain("Godot", File.ReadAllText(Path.Combine(root, "src", "Sim", "Rts.Sim.csproj")));
    }

    [Fact]
    public void ExportPreset_IsLinuxWithDataFilter()
    {
        // UT-BE-165
        var root = ForbiddenApiScanner.FindRepoRoot();
        var cfg = File.ReadAllLines(Path.Combine(root, "game", "export_presets.cfg")).Select(l => l.TrimEnd('\r')).ToArray();
        var gi = File.ReadAllLines(Path.Combine(root, ".gitignore")).Select(l => l.TrimEnd('\r')).ToArray();
        foreach (var line in new[]
        {
            "name=\"Linux\"",
            "platform=\"Linux\"",
            "include_filter=\"data/*.json\"",
            "export_path=\"../build/linux/Rts.x86_64\"",
            "binary_format/embed_pck=false",
            "binary_format/architecture=\"x86_64\"",
        })
        {
            Assert.Contains(line, cfg);
        }

        Assert.Equal(
            1,
            cfg.Count(l => l.StartsWith("[preset.", StringComparison.Ordinal) && l.EndsWith("]", StringComparison.Ordinal)
                && !l.Contains(".options", StringComparison.Ordinal)));
        Assert.Contains("build/", gi);
    }
}
