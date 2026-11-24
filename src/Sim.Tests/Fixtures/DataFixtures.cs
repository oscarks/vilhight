using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Rts.Sim.Data;

namespace Rts.Sim.Tests.Fixtures;

public static class DataFixtures
{
    public const string BaseResources = """
        {"resources":[
          {"id":"r_grain","category":"basic","storage":"stock","renewable":true},
          {"id":"r_lore","category":"abstract","storage":"stock"},
          {"id":"r_rock","category":"raw_hidden","storage":"stock","revealedBy":"t_dig","extractedBy":"b_pit","depletes":true},
          {"id":"r_alloy","category":"refined","storage":"stock"},
          {"id":"r_gem","category":"strategic","storage":"quota","revealedBy":"t_gem","quotaPerSource":4}
        ]}
        """;

    public const string BaseTechs = """
        {"techs":[
          {"id":"t_dig","tier":1,"cost":{"r_lore":50},"researchTicks":600,"requires":[],
           "effects":[{"type":"revealResource","resource":"r_rock"},{"type":"unlockBuilding","building":"b_pit"}]},
          {"id":"t_melt","tier":2,"cost":{"r_rock":50,"r_lore":120},"researchTicks":900,"requires":["t_dig"],
           "effects":[{"type":"unlockRecipe","recipe":"x_melt"},{"type":"raiseGatherCap","resource":"r_grain","percent":25},
                      {"type":"unlockUnit","unit":"u_guard"}]},
          {"id":"t_gem","tier":3,"cost":{"r_lore":200,"r_alloy":10},"researchTicks":1200,"requires":["t_melt"],
           "effects":[{"type":"revealResource","resource":"r_gem"},
                      {"type":"modifyStat","unit":"u_guard","stat":"armor","add":1},
                      {"type":"replaceRequirement","from":"r_grain","to":"r_alloy"},
                      {"type":"obsolete","unit":"u_guard"}]}
        ]}
        """;

    public const string OneResource = """{"resources":[{"id":"r_a","category":"basic","storage":"stock"}]}""";
    public const string NoTechs = """{"techs":[]}""";

    public static RawData Raw(string? resources, string? techs) => new(resources, techs);

    public static RawData Base() => new(BaseResources, BaseTechs);

    public static string Resources(params string[] items) => "{\"resources\":[" + string.Join(",", items) + "]}";

    public static string Techs(params string[] items) => "{\"techs\":[" + string.Join(",", items) + "]}";

    // Tech mínima válida; "extra" substitui/acrescenta campos (pares "nome":valor já em JSON).
    public static string T1(string? id = null, string? tier = null, string? cost = null, string? ticks = null,
        string? requires = null, string? effects = null, string? extra = null, bool omitEffects = false)
    {
        var sb = new StringBuilder("{");
        sb.Append("\"id\":\"").Append(id ?? "t_a").Append("\",");
        sb.Append("\"tier\":").Append(tier ?? "0").Append(',');
        sb.Append("\"cost\":").Append(cost ?? "{}").Append(',');
        sb.Append("\"researchTicks\":").Append(ticks ?? "1").Append(',');
        sb.Append("\"requires\":").Append(requires ?? "[]");
        if (!omitEffects)
        {
            sb.Append(",\"effects\":").Append(effects ?? "[]");
        }

        if (extra != null)
        {
            sb.Append(',').Append(extra);
        }

        sb.Append('}');
        return sb.ToString();
    }

    public static string Dump(DataSet data)
    {
        var sb = new StringBuilder();
        foreach (var r in data.Resources.All)
        {
            sb.Append("R ").Append(r.Id.Value).Append(' ').Append(r.Key).Append(' ').Append(r.Category).Append(' ')
                .Append(r.Storage).Append(' ').Append(r.Renewable).Append(' ').Append(r.Depletes).Append(' ')
                .Append(r.RevealedBy?.Value.ToString() ?? "-").Append(' ').Append(r.ExtractedBy ?? "-").Append(' ')
                .Append(r.QuotaPerSource).Append('\n');
        }

        foreach (var t in data.Techs.All)
        {
            sb.Append("T ").Append(t.Id.Value).Append(' ').Append(t.Key).Append(' ').Append(t.Tier).Append(' ')
                .Append(t.ResearchTicks).Append(" cost[");
            foreach (var c in t.Cost)
            {
                sb.Append(c.Resource.Value).Append('=').Append(c.Amount.Raw).Append(';');
            }

            sb.Append("] req[");
            foreach (var q in t.Requires)
            {
                sb.Append(q.Value).Append(';');
            }

            sb.Append("]\n");
            foreach (var e in t.Effects)
            {
                sb.Append("  E ").Append(e).Append('\n');
            }
        }

        foreach (var w in data.Warnings)
        {
            sb.Append("W ").Append(w).Append('\n');
        }

        foreach (var p in data.PendingReferences)
        {
            sb.Append("P ").Append(p).Append('\n');
        }

        return sb.ToString();
    }

    public static TempDataDir WriteTemp(string? resources, string? techs)
    {
        var dir = Directory.CreateTempSubdirectory("rts-data-");
        if (resources != null)
        {
            File.WriteAllText(Path.Combine(dir.FullName, DataFiles.Resources), resources, new UTF8Encoding(false));
        }

        if (techs != null)
        {
            File.WriteAllText(Path.Combine(dir.FullName, DataFiles.Techs), techs, new UTF8Encoding(false));
        }

        return new TempDataDir(dir.FullName);
    }

    public static IReadOnlyList<(DataErrorCode Code, string File, string Path, string? Id)> Errs(RawData raw)
    {
        var result = new List<(DataErrorCode, string, string, string?)>();
        foreach (var e in DataValidator.Validate(raw))
        {
            if (e.Severity == DataSeverity.Error)
            {
                result.Add((e.Code, e.File, e.Path, e.Id));
            }
        }

        return result;
    }
}

public sealed class TempDataDir : IDisposable
{
    public TempDataDir(string path) => Path = path;

    public string Path { get; }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, true);
        }
        catch (IOException)
        {
        }
    }
}
