using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Rts.Sim.Data;

namespace Rts.Sim.Tests.Architecture;

// Varredura de literais de id de conteúdo em src/Sim (ADR-005; AI-SPEC Fase 3 RN-P3-63).
public static class ContentIdScanner
{
    public sealed record Literal(int Line, string Value);

    public static IReadOnlyList<Literal> FindStringLiterals(string strippedSource)
    {
        var result = new List<Literal>();
        var line = 1;
        var i = 0;
        while (i < strippedSource.Length)
        {
            var ch = strippedSource[i];
            if (ch == '\n')
            {
                line++;
                i++;
            }
            else if (ch == '\'')
            {
                i++;
                while (i < strippedSource.Length && strippedSource[i] != '\'' && strippedSource[i] != '\n')
                {
                    i += strippedSource[i] == '\\' ? 2 : 1;
                }

                i++;
            }
            else if (ch == '"')
            {
                var startLine = line;
                var sb = new StringBuilder();
                i++;
                while (i < strippedSource.Length && strippedSource[i] != '"' && strippedSource[i] != '\n')
                {
                    if (strippedSource[i] == '\\' && i + 1 < strippedSource.Length)
                    {
                        i++;
                    }

                    sb.Append(strippedSource[i]);
                    i++;
                }

                i++;
                result.Add(new Literal(startLine, sb.ToString()));
            }
            else
            {
                i++;
            }
        }

        return result;
    }

    public static IReadOnlyList<string> ContentKeys(DataSet data)
    {
        var set = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var r in data.Resources.All)
        {
            set.Add(r.Key);
        }

        foreach (var t in data.Techs.All)
        {
            set.Add(t.Key);
        }

        foreach (var p in data.PendingReferences)
        {
            set.Add(p.TargetKey);
        }

        return new List<string>(set);
    }

    public static bool Matches(string literal, string key) =>
        Regex.IsMatch(literal, "(?<![a-z0-9_])" + Regex.Escape(key) + "(?![a-z0-9_])");

    public static IReadOnlyList<string> Scan(string repoRoot, string relativeDir, IReadOnlyList<string> keys)
    {
        var result = new List<string>();
        foreach (var rel in ForbiddenApiScanner.ListSourceFiles(repoRoot, relativeDir))
        {
            var stripped = ForbiddenApiScanner.StripComments(File.ReadAllText(Path.Combine(repoRoot, rel)));
            foreach (var lit in FindStringLiterals(stripped))
            {
                foreach (var key in keys)
                {
                    if (Matches(lit.Value, key))
                    {
                        result.Add($"{rel}:{lit.Line}: CID {key} in \"{lit.Value}\"");
                    }
                }
            }
        }

        return result;
    }
}
