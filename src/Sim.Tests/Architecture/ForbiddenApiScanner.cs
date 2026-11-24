using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Rts.Sim.Tests.Architecture;

// Varredura textual de APIs proibidas em src/Sim (ADR-001, ADR-002; AI-SPEC Fase 2 RN-P2-70).
public static class ForbiddenApiScanner
{
    private static readonly (string Rule, Regex Pattern)[] Rules =
    {
        ("FB-01", new Regex(@"\b(float|double|decimal|Half)\b")),
        ("FB-02", new Regex(@"\bMathF\b|\bMath\.(Sqrt|Pow|Exp|Log|Log2|Log10|Sin|Cos|Tan|Asin|Acos|Atan|Atan2|Sinh|Cosh|Tanh|Round|Floor|Ceiling|Truncate|Cbrt)\b")),
        ("FB-03", new Regex(@"System\.Random|\bnew\s+Random\s*\(|\bRandom\.Shared\b")),
        ("FB-04", new Regex(@"\bDateTime\b|\bDateTimeOffset\b|\bStopwatch\b|\bTimeProvider\b|Environment\.TickCount")),
        ("FB-05", new Regex(@"Guid\.NewGuid")),
        ("FB-06", new Regex(@"\bThread\b|\bThreadPool\b|Task\.Run|\bParallel\.|\basync\b|\bawait\b")),
        ("FB-07", new Regex(@"using\s+Godot|\bGodot\.|\bGD\.|res://")),
        ("FB-08", new Regex(@"\.GetHashCode\s*\(|\bHashCode\.")),
        ("FB-09", new Regex(@"^\s*((public|private|internal|protected)\s+)*static\s+(?!readonly\b|class\b|partial\b|void\b)[\w<>\[\],?. ]+\s+\w+\s*(=(?!>)|;)")),
    };

    public sealed record Violation(string Path, int Line, string Rule, string Text)
    {
        public override string ToString() => $"{Path}:{Line}: {Rule} {Text}";
    }

    public static string StripComments(string source)
    {
        var sb = new StringBuilder(source.Length);
        var i = 0;
        while (i < source.Length)
        {
            var ch = source[i];
            var next = i + 1 < source.Length ? source[i + 1] : '\0';
            if (ch == '"' || ch == '\'')
            {
                sb.Append(ch);
                i++;
                while (i < source.Length)
                {
                    var c = source[i];
                    sb.Append(c);
                    i++;
                    if (c == '\\' && i < source.Length)
                    {
                        sb.Append(source[i]);
                        i++;
                    }
                    else if (c == ch || c == '\n')
                    {
                        break;
                    }
                }
            }
            else if (ch == '/' && next == '/')
            {
                while (i < source.Length && source[i] != '\n')
                {
                    sb.Append(source[i] == '\r' ? '\r' : ' ');
                    i++;
                }
            }
            else if (ch == '/' && next == '*')
            {
                sb.Append("  ");
                i += 2;
                while (i < source.Length && !(source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/'))
                {
                    sb.Append(source[i] == '\n' || source[i] == '\r' ? source[i] : ' ');
                    i++;
                }

                if (i < source.Length)
                {
                    sb.Append("  ");
                    i += 2;
                }
            }
            else
            {
                sb.Append(ch);
                i++;
            }
        }

        return sb.ToString();
    }

    public static IReadOnlyList<Violation> ScanSource(string relativePath, string source)
    {
        var result = new List<Violation>();
        var lines = StripComments(source).Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            foreach (var (rule, pattern) in Rules)
            {
                if (pattern.IsMatch(line))
                {
                    result.Add(new Violation(relativePath, i + 1, rule, line.Trim()));
                }
            }
        }

        return result;
    }

    public static IReadOnlyList<string> ListSourceFiles(string repoRoot, string relativeDir)
    {
        var dir = Path.Combine(repoRoot, relativeDir);
        var result = new List<string>();
        foreach (var file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
            var segments = rel.Split('/');
            var skip = false;
            foreach (var seg in segments)
            {
                if (seg == "bin" || seg == "obj")
                {
                    skip = true;
                }
            }

            if (!skip)
            {
                result.Add(rel);
            }
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    public static IReadOnlyList<Violation> ScanDirectory(string repoRoot, string relativeDir)
    {
        var result = new List<Violation>();
        foreach (var rel in ListSourceFiles(repoRoot, relativeDir))
        {
            result.AddRange(ScanSource(rel, File.ReadAllText(Path.Combine(repoRoot, rel))));
        }

        return result;
    }

    public static string FindRepoRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Rts.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Rts.sln not found above " + baseDir);
    }
}
