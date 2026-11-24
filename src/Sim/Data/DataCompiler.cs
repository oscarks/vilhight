using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Data;

// Etapas 1–6 do AI-SPEC Foundation Fase 3 §5.1. Coleta todos os erros (RN-P3-01) em ordem determinística (RN-P3-02).
internal sealed partial class DataCompiler
{
    internal sealed class Result
    {
        public Result(DataError[] items, DataSet? set)
        {
            Items = items;
            Set = set;
        }

        public DataError[] Items { get; }
        public DataSet? Set { get; }
    }

    private sealed class ResEntry
    {
        public string? Id;
        public ResourceCategory Category;
        public ResourceStorage Storage;
        public bool Renewable;
        public bool Depletes;
        public string? RevealedBy;
        public string? ExtractedBy;
        public int Quota;
    }

    private readonly record struct CostItem(string Key, int Value);

    private readonly record struct RefItem(string Key, int Index);

    private sealed class EffectEntry
    {
        public int Index;
        public TechEffectType Type;
        public string? Res1;
        public string Res1Prop = "resource";
        public string? Res2;
        public string? Target;
        public string TargetProp = "";
        public ContentKind Kind;
        public string? Stat;
        public StatModifierKind Mod;
        public int Value;
    }

    private sealed class TechEntry
    {
        public string? Id;
        public int Tier;
        public int Ticks;
        public readonly List<CostItem> Cost = new();
        public readonly List<RefItem> Requires = new();
        public readonly List<EffectEntry> Effects = new();
    }

    private readonly List<DataError> _items = new();
    private readonly List<PendingReference> _pending = new();
    private int _errorCount;
    private string _file = "";

    private readonly List<ResEntry?> _res = new();
    private readonly List<TechEntry?> _techs = new();
    private readonly Dictionary<string, int> _resMap = new(StringComparer.Ordinal);   // só busca
    private readonly Dictionary<string, int> _techMap = new(StringComparer.Ordinal);  // só busca

    public static Result Compile(RawData raw, long resourcesOversizeBytes, long techsOversizeBytes)
    {
        var c = new DataCompiler();
        return c.Run(raw, resourcesOversizeBytes, techsOversizeBytes);
    }

    private Result Run(RawData raw, long resOversize, long techOversize)
    {
        JsonDocument? resDoc = null;
        JsonDocument? techDoc = null;
        try
        {
            // Etapas 1 e 2: recursos, depois techs.
            var resList = Stage1(DataFiles.Resources, raw.ResourcesJson, resOversize, "resources", out resDoc);
            if (resList.HasValue)
            {
                ParseResources(resList.Value);
            }

            var techList = Stage1(DataFiles.Techs, raw.TechsJson, techOversize, "techs", out techDoc);
            if (techList.HasValue)
            {
                ParseTechs(techList.Value);
            }

            var usableRes = resList.HasValue;
            var usableTech = techList.HasValue;

            if (usableRes && usableTech)
            {
                Stage3();
                Stage4();
            }

            if (usableTech)
            {
                Stage5();
            }

            DataSet? set = null;
            if (_errorCount == 0)
            {
                set = Assemble();
                Stage6(set);
                if (_errorCount > 0)
                {
                    set = null;
                }
            }

            return new Result(_items.ToArray(), set);
        }
        finally
        {
            resDoc?.Dispose();
            techDoc?.Dispose();
        }
    }

    // ---------- emissão ----------

    private void Err(DataErrorCode code, string path, string? id, string message)
    {
        _items.Add(new DataError(DataSeverity.Error, code, _file, path, id, message));
        _errorCount++;
    }

    private void Warn(string file, string path, string? id, string message) =>
        _items.Add(new DataError(DataSeverity.Warning, DataErrorCode.PendingReference, file, path, id, message));

    private static string KindName(JsonValueKind kind) => kind switch
    {
        JsonValueKind.Object => "object",
        JsonValueKind.Array => "array",
        JsonValueKind.String => "string",
        JsonValueKind.Number => "number",
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => "null",
    };

    private void Unexpected(string path, string? id, string expected, JsonElement v) =>
        Err(DataErrorCode.UnexpectedType, path, id, "expected " + expected + ", got " + KindName(v.ValueKind));

    // RN-P3-10
    internal static bool IsValidId(string s)
    {
        if (s.Length < 1 || s.Length > DataLimits.MaxIdLength)
        {
            return false;
        }

        if (s[0] < 'a' || s[0] > 'z')
        {
            return false;
        }

        for (var i = 1; i < s.Length; i++)
        {
            var ch = s[i];
            if (!((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private string? ReadId(JsonElement v, string path, string? owner)
    {
        if (v.ValueKind != JsonValueKind.String)
        {
            Unexpected(path, owner, "string", v);
            return null;
        }

        var s = v.GetString()!;
        if (!IsValidId(s))
        {
            Err(DataErrorCode.InvalidId, path, owner, "invalid id '" + s + "'");
            return null;
        }

        return s;
    }

    private bool ReadInt(JsonElement v, string path, string? owner, out int value)
    {
        value = 0;
        if (v.ValueKind != JsonValueKind.Number)
        {
            Unexpected(path, owner, "integer", v);
            return false;
        }

        if (!v.TryGetInt32(out value))
        {
            Err(DataErrorCode.InvalidNumber, path, owner, "expected a 32-bit integer, got " + v.GetRawText());
            return false;
        }

        return true;
    }

    private bool ReadBool(JsonElement v, string path, string? owner, out bool value)
    {
        value = false;
        if (v.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (v.ValueKind == JsonValueKind.False)
        {
            return true;
        }

        Unexpected(path, owner, "boolean", v);
        return false;
    }

    // Retorna o texto se for string; senão emite UnexpectedType.
    private string? ReadString(JsonElement v, string path, string? owner)
    {
        if (v.ValueKind != JsonValueKind.String)
        {
            Unexpected(path, owner, "string", v);
            return null;
        }

        return v.GetString();
    }

    private void MinInt(int v, int min, string name, string path, string? owner)
    {
        if (v < min)
        {
            Err(DataErrorCode.OutOfRange, path, owner,
                name + " must be >= " + min.ToString(CultureInfo.InvariantCulture) + ", got " + v.ToString(CultureInfo.InvariantCulture));
        }
    }

    // Id da entrada: primeira propriedade "id" se for string com formato válido (RN-P3-02).
    private static string? OwnerId(JsonElement obj)
    {
        foreach (var p in obj.EnumerateObject())
        {
            if (p.Name == "id")
            {
                if (p.Value.ValueKind == JsonValueKind.String)
                {
                    var s = p.Value.GetString()!;
                    return IsValidId(s) ? s : null;
                }

                return null;
            }
        }

        return null;
    }

    private static int Slot(string[] schema, string name)
    {
        for (var i = 0; i < schema.Length; i++)
        {
            if (schema[i] == name)
            {
                return i;
            }
        }

        return -1;
    }
}
