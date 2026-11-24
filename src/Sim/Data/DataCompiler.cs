using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Data;

// Etapas 1–6 do AI-SPEC Foundation Fase 3 §5.1. Coleta todos os erros (RN-P3-01) em ordem determinística (RN-P3-02).
internal sealed class DataCompiler
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

    // ---------- etapa 1 ----------

    private JsonElement? Stage1(string file, string? text, long oversize, string listName, out JsonDocument? doc)
    {
        doc = null;
        _file = file;
        if (oversize >= 0)
        {
            Err(DataErrorCode.FileTooLarge, JsonPath.Root, null, TooLarge(oversize));
            return null;
        }

        if (text == null)
        {
            Err(DataErrorCode.FileMissing, JsonPath.Root, null, "file not found");
            return null;
        }

        var bytes = Encoding.UTF8.GetByteCount(text);
        if (bytes > DataLimits.MaxFileBytes)
        {
            Err(DataErrorCode.FileTooLarge, JsonPath.Root, null, TooLarge(bytes));
            return null;
        }

        try
        {
            doc = JsonDocument.Parse(text, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = DataLimits.MaxJsonDepth,
            });
        }
        catch (JsonException ex)
        {
            Err(DataErrorCode.InvalidJson, JsonPath.Root, null, "invalid JSON: " + ex.Message);
            return null;
        }

        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            Unexpected(JsonPath.Root, null, "object", root);
            return null;
        }

        JsonElement? list = null;
        var seen = false;
        var listPath = JsonPath.Prop(JsonPath.Root, listName);
        foreach (var p in root.EnumerateObject())
        {
            var ppath = JsonPath.Prop(JsonPath.Root, p.Name);
            if (p.Name != listName)
            {
                Err(DataErrorCode.UnknownProperty, ppath, null, "unknown property '" + p.Name + "'");
            }
            else if (seen)
            {
                Err(DataErrorCode.DuplicateProperty, ppath, null, "duplicate property '" + p.Name + "'");
            }
            else
            {
                seen = true;
                if (p.Value.ValueKind == JsonValueKind.Array)
                {
                    list = p.Value;
                }
                else
                {
                    Unexpected(listPath, null, "array", p.Value);
                }
            }
        }

        if (!seen)
        {
            Err(DataErrorCode.MissingProperty, listPath, null, "missing required property '" + listName + "'");
        }

        return list;
    }

    private static string TooLarge(long bytes) =>
        "file has " + bytes.ToString(CultureInfo.InvariantCulture) + " bytes (max " +
        DataLimits.MaxFileBytes.ToString(CultureInfo.InvariantCulture) + ")";

    // ---------- etapa 2: recursos ----------

    private static string[] ResourceSchema() =>
        new[] { "id", "category", "storage", "renewable", "depletes", "revealedBy", "extractedBy", "quotaPerSource" };

    private void ParseResources(JsonElement list)
    {
        _file = DataFiles.Resources;
        var i = 0;
        foreach (var el in list.EnumerateArray())
        {
            var path = JsonPath.Index("$.resources", i);
            if (el.ValueKind != JsonValueKind.Object)
            {
                Unexpected(path, null, "object", el);
                _res.Add(null);
            }
            else
            {
                _res.Add(ParseResource(el, path, i));
            }

            i++;
        }
    }

    private ResEntry ParseResource(JsonElement el, string path, int index)
    {
        var owner = OwnerId(el);
        var schema = ResourceSchema();
        var seen = new bool[schema.Length];
        var e = new ResEntry();
        var categoryOk = false;
        var storageOk = false;
        foreach (var p in el.EnumerateObject())
        {
            var ppath = JsonPath.Prop(path, p.Name);
            var slot = Slot(schema, p.Name);
            if (slot < 0)
            {
                Err(DataErrorCode.UnknownProperty, ppath, owner, "unknown property '" + p.Name + "'");
                continue;
            }

            if (seen[slot])
            {
                Err(DataErrorCode.DuplicateProperty, ppath, owner, "duplicate property '" + p.Name + "'");
                continue;
            }

            seen[slot] = true;
            switch (slot)
            {
                case 0:
                    e.Id = ReadId(p.Value, ppath, owner);
                    break;
                case 1:
                    {
                        var s = ReadString(p.Value, ppath, owner);
                        if (s != null)
                        {
                            switch (s)
                            {
                                case "basic": e.Category = ResourceCategory.Basic; categoryOk = true; break;
                                case "raw_hidden": e.Category = ResourceCategory.RawHidden; categoryOk = true; break;
                                case "refined": e.Category = ResourceCategory.Refined; categoryOk = true; break;
                                case "strategic": e.Category = ResourceCategory.Strategic; categoryOk = true; break;
                                case "abstract": e.Category = ResourceCategory.Abstract; categoryOk = true; break;
                                default: Err(DataErrorCode.InvalidEnumValue, ppath, owner, "invalid value '" + s + "' for 'category'"); break;
                            }
                        }

                        break;
                    }

                case 2:
                    {
                        var s = ReadString(p.Value, ppath, owner);
                        if (s != null)
                        {
                            switch (s)
                            {
                                case "stock": e.Storage = ResourceStorage.Stock; storageOk = true; break;
                                case "quota": e.Storage = ResourceStorage.Quota; storageOk = true; break;
                                default: Err(DataErrorCode.InvalidEnumValue, ppath, owner, "invalid value '" + s + "' for 'storage'"); break;
                            }
                        }

                        break;
                    }

                case 3:
                    ReadBool(p.Value, ppath, owner, out e.Renewable);
                    break;
                case 4:
                    ReadBool(p.Value, ppath, owner, out e.Depletes);
                    break;
                case 5:
                    e.RevealedBy = ReadId(p.Value, ppath, owner);
                    break;
                case 6:
                    e.ExtractedBy = ReadId(p.Value, ppath, owner);
                    break;
                default:
                    if (ReadInt(p.Value, ppath, owner, out var q))
                    {
                        MinInt(q, 1, "quotaPerSource", ppath, owner);
                        e.Quota = q;
                    }

                    break;
            }
        }

        for (var s = 0; s < 3; s++)
        {
            if (!seen[s])
            {
                Err(DataErrorCode.MissingProperty, JsonPath.Prop(path, schema[s]), owner, "missing required property '" + schema[s] + "'");
            }
        }

        if (storageOk)
        {
            if (e.Storage == ResourceStorage.Quota && !seen[7])
            {
                Err(DataErrorCode.QuotaMismatch, path, owner, "storage 'quota' requires quotaPerSource");
            }
            else if (e.Storage == ResourceStorage.Stock && seen[7])
            {
                Err(DataErrorCode.QuotaMismatch, JsonPath.Prop(path, "quotaPerSource"), owner, "quotaPerSource is only allowed with storage 'quota'");
            }
        }

        if (categoryOk && e.Category == ResourceCategory.RawHidden && !seen[5])
        {
            Err(DataErrorCode.MissingRevealedBy, path, owner, "category 'raw_hidden' requires revealedBy");
        }

        if (e.Id != null)
        {
            if (_resMap.TryGetValue(e.Id, out var first))
            {
                Err(DataErrorCode.DuplicateId, JsonPath.Prop(path, "id"), owner,
                    "duplicate id '" + e.Id + "' (first at index " + first.ToString(CultureInfo.InvariantCulture) + ")");
            }
            else
            {
                _resMap[e.Id] = index;
            }
        }

        return e;
    }

    // ---------- etapa 2: techs ----------

    private static string[] TechSchema() => new[] { "id", "tier", "cost", "researchTicks", "requires", "effects" };

    private void ParseTechs(JsonElement list)
    {
        _file = DataFiles.Techs;
        var i = 0;
        foreach (var el in list.EnumerateArray())
        {
            var path = JsonPath.Index("$.techs", i);
            if (el.ValueKind != JsonValueKind.Object)
            {
                Unexpected(path, null, "object", el);
                _techs.Add(null);
            }
            else
            {
                _techs.Add(ParseTech(el, path, i));
            }

            i++;
        }
    }

    private TechEntry ParseTech(JsonElement el, string path, int index)
    {
        var owner = OwnerId(el);
        var schema = TechSchema();
        var seen = new bool[schema.Length];
        var t = new TechEntry();
        foreach (var p in el.EnumerateObject())
        {
            var ppath = JsonPath.Prop(path, p.Name);
            var slot = Slot(schema, p.Name);
            if (slot < 0)
            {
                Err(DataErrorCode.UnknownProperty, ppath, owner, "unknown property '" + p.Name + "'");
                continue;
            }

            if (seen[slot])
            {
                Err(DataErrorCode.DuplicateProperty, ppath, owner, "duplicate property '" + p.Name + "'");
                continue;
            }

            seen[slot] = true;
            switch (slot)
            {
                case 0:
                    t.Id = ReadId(p.Value, ppath, owner);
                    break;
                case 1:
                    if (ReadInt(p.Value, ppath, owner, out var tier))
                    {
                        MinInt(tier, 0, "tier", ppath, owner);
                        t.Tier = tier;
                    }

                    break;
                case 2:
                    ParseCost(p.Value, ppath, owner, t);
                    break;
                case 3:
                    if (ReadInt(p.Value, ppath, owner, out var ticks))
                    {
                        MinInt(ticks, 1, "researchTicks", ppath, owner);
                        t.Ticks = ticks;
                    }

                    break;
                case 4:
                    ParseRequires(p.Value, ppath, owner, t);
                    break;
                default:
                    ParseEffects(p.Value, ppath, owner, t);
                    break;
            }
        }

        for (var s = 0; s < schema.Length; s++)
        {
            if (!seen[s])
            {
                Err(DataErrorCode.MissingProperty, JsonPath.Prop(path, schema[s]), owner, "missing required property '" + schema[s] + "'");
            }
        }

        if (t.Id != null)
        {
            if (_techMap.TryGetValue(t.Id, out var first))
            {
                Err(DataErrorCode.DuplicateId, JsonPath.Prop(path, "id"), owner,
                    "duplicate id '" + t.Id + "' (first at index " + first.ToString(CultureInfo.InvariantCulture) + ")");
            }
            else
            {
                _techMap[t.Id] = index;
            }
        }

        return t;
    }

    private void ParseCost(JsonElement v, string path, string? owner, TechEntry t)
    {
        if (v.ValueKind != JsonValueKind.Object)
        {
            Unexpected(path, owner, "object", v);
            return;
        }

        foreach (var p in v.EnumerateObject())
        {
            var ppath = JsonPath.Prop(path, p.Name);
            if (!IsValidId(p.Name))
            {
                Err(DataErrorCode.InvalidId, ppath, owner, "invalid id '" + p.Name + "'");
                continue;
            }

            var dup = false;
            foreach (var c in t.Cost)
            {
                if (c.Key == p.Name)
                {
                    dup = true;
                }
            }

            if (dup)
            {
                Err(DataErrorCode.DuplicateProperty, ppath, owner, "duplicate property '" + p.Name + "'");
                continue;
            }

            if (ReadInt(p.Value, ppath, owner, out var n))
            {
                MinInt(n, 1, "cost", ppath, owner);
                t.Cost.Add(new CostItem(p.Name, n));
            }
            else
            {
                // chave válida com valor inválido: mantém a referência para o mapa, sem custo útil.
                t.Cost.Add(new CostItem(p.Name, 0));
            }
        }
    }

    private void ParseRequires(JsonElement v, string path, string? owner, TechEntry t)
    {
        if (v.ValueKind != JsonValueKind.Array)
        {
            Unexpected(path, owner, "array", v);
            return;
        }

        var j = 0;
        foreach (var item in v.EnumerateArray())
        {
            var ipath = JsonPath.Index(path, j);
            var key = ReadId(item, ipath, owner);
            if (key != null)
            {
                var dup = false;
                foreach (var r in t.Requires)
                {
                    if (r.Key == key)
                    {
                        dup = true;
                    }
                }

                if (dup)
                {
                    Err(DataErrorCode.DuplicateReference, ipath, owner, "duplicate reference '" + key + "'");
                }
                else
                {
                    t.Requires.Add(new RefItem(key, j));
                }
            }

            j++;
        }
    }

    private void ParseEffects(JsonElement v, string path, string? owner, TechEntry t)
    {
        if (v.ValueKind != JsonValueKind.Array)
        {
            Unexpected(path, owner, "array", v);
            return;
        }

        var j = 0;
        foreach (var item in v.EnumerateArray())
        {
            var epath = JsonPath.Index(path, j);
            if (item.ValueKind != JsonValueKind.Object)
            {
                Unexpected(epath, owner, "object", item);
            }
            else
            {
                ParseEffect(item, epath, owner, j, t);
            }

            j++;
        }
    }

    private static string[] EffectSchema(TechEffectType type) => type switch
    {
        TechEffectType.RevealResource => new[] { "type", "resource" },
        TechEffectType.UnlockBuilding => new[] { "type", "building" },
        TechEffectType.UnlockUnit => new[] { "type", "unit" },
        TechEffectType.UnlockRecipe => new[] { "type", "recipe" },
        TechEffectType.ModifyStat => new[] { "type", "stat", "unit", "building", "add", "percent" },
        TechEffectType.RaiseGatherCap => new[] { "type", "resource", "percent" },
        TechEffectType.ReplaceRequirement => new[] { "type", "from", "to" },
        _ => new[] { "type", "unit", "recipe" },
    };

    private static bool TryEffectType(string s, out TechEffectType type)
    {
        switch (s)
        {
            case "revealResource": type = TechEffectType.RevealResource; return true;
            case "unlockBuilding": type = TechEffectType.UnlockBuilding; return true;
            case "unlockUnit": type = TechEffectType.UnlockUnit; return true;
            case "unlockRecipe": type = TechEffectType.UnlockRecipe; return true;
            case "modifyStat": type = TechEffectType.ModifyStat; return true;
            case "raiseGatherCap": type = TechEffectType.RaiseGatherCap; return true;
            case "replaceRequirement": type = TechEffectType.ReplaceRequirement; return true;
            case "obsolete": type = TechEffectType.Obsolete; return true;
            default: type = default; return false;
        }
    }

    private void ParseEffect(JsonElement el, string path, string? owner, int index, TechEntry t)
    {
        // RN-P3-24: "type" primeiro; sem tipo válido o resto é ignorado.
        JsonElement typeEl = default;
        var hasType = false;
        foreach (var p in el.EnumerateObject())
        {
            if (p.Name == "type")
            {
                typeEl = p.Value;
                hasType = true;
                break;
            }
        }

        var typePath = JsonPath.Prop(path, "type");
        if (!hasType)
        {
            Err(DataErrorCode.MissingProperty, typePath, owner, "missing required property 'type'");
            return;
        }

        var typeText = ReadString(typeEl, typePath, owner);
        if (typeText == null)
        {
            return;
        }

        if (!TryEffectType(typeText, out var type))
        {
            Err(DataErrorCode.UnknownEffectType, typePath, owner, "unknown effect type '" + typeText + "'");
            return;
        }

        var schema = EffectSchema(type);
        var seen = new bool[schema.Length];
        var fx = new EffectEntry { Index = index, Type = type };
        foreach (var p in el.EnumerateObject())
        {
            var ppath = JsonPath.Prop(path, p.Name);
            var slot = Slot(schema, p.Name);
            if (slot < 0)
            {
                Err(DataErrorCode.UnknownProperty, ppath, owner, "unknown property '" + p.Name + "'");
                continue;
            }

            if (seen[slot])
            {
                Err(DataErrorCode.DuplicateProperty, ppath, owner, "duplicate property '" + p.Name + "'");
                continue;
            }

            seen[slot] = true;
            if (slot == 0)
            {
                continue;
            }

            switch (type)
            {
                case TechEffectType.RevealResource:
                case TechEffectType.RaiseGatherCap when slot == 1:
                    fx.Res1 = ReadId(p.Value, ppath, owner);
                    fx.Res1Prop = "resource";
                    break;
                case TechEffectType.UnlockBuilding:
                    fx.Target = ReadId(p.Value, ppath, owner);
                    fx.TargetProp = "building";
                    fx.Kind = ContentKind.Building;
                    break;
                case TechEffectType.UnlockUnit:
                    fx.Target = ReadId(p.Value, ppath, owner);
                    fx.TargetProp = "unit";
                    fx.Kind = ContentKind.Unit;
                    break;
                case TechEffectType.UnlockRecipe:
                    fx.Target = ReadId(p.Value, ppath, owner);
                    fx.TargetProp = "recipe";
                    fx.Kind = ContentKind.Recipe;
                    break;
                case TechEffectType.RaiseGatherCap:
                    if (ReadInt(p.Value, ppath, owner, out var pct))
                    {
                        MinInt(pct, 1, "percent", ppath, owner);
                        fx.Value = pct;
                    }

                    break;
                case TechEffectType.ReplaceRequirement:
                    if (slot == 1)
                    {
                        fx.Res1 = ReadId(p.Value, ppath, owner);
                        fx.Res1Prop = "from";
                    }
                    else
                    {
                        fx.Res2 = ReadId(p.Value, ppath, owner);
                    }

                    break;
                case TechEffectType.ModifyStat:
                    switch (slot)
                    {
                        case 1:
                            fx.Stat = ReadId(p.Value, ppath, owner);
                            break;
                        case 2:
                        case 3:
                            fx.Target = ReadId(p.Value, ppath, owner);
                            fx.TargetProp = schema[slot];
                            fx.Kind = slot == 2 ? ContentKind.Unit : ContentKind.Building;
                            break;
                        default:
                            if (ReadInt(p.Value, ppath, owner, out var amount))
                            {
                                if (amount == 0)
                                {
                                    Err(DataErrorCode.OutOfRange, ppath, owner, schema[slot] + " must not be 0");
                                }

                                fx.Value = amount;
                                fx.Mod = slot == 4 ? StatModifierKind.Add : StatModifierKind.Percent;
                            }

                            break;
                    }

                    break;
                default: // Obsolete
                    fx.Target = ReadId(p.Value, ppath, owner);
                    fx.TargetProp = schema[slot];
                    fx.Kind = slot == 1 ? ContentKind.Unit : ContentKind.Recipe;
                    break;
            }
        }

        // MissingProperty na ordem da tabela (só obrigatórias; os pares exclusivos vêm depois).
        for (var s = 1; s < schema.Length; s++)
        {
            if (seen[s] || IsOptionalPair(type, s))
            {
                continue;
            }

            Err(DataErrorCode.MissingProperty, JsonPath.Prop(path, schema[s]), owner, "missing required property '" + schema[s] + "'");
        }

        if (type == TechEffectType.ModifyStat)
        {
            if (Count(seen, 2, 3) != 1)
            {
                Err(DataErrorCode.ExactlyOneRequired, path, owner, "exactly one of 'unit', 'building' is required");
            }

            if (Count(seen, 4, 5) != 1)
            {
                Err(DataErrorCode.ExactlyOneRequired, path, owner, "exactly one of 'add', 'percent' is required");
            }
        }
        else if (type == TechEffectType.Obsolete && Count(seen, 1, 2) != 1)
        {
            Err(DataErrorCode.ExactlyOneRequired, path, owner, "exactly one of 'unit', 'recipe' is required");
        }

        if (type == TechEffectType.ReplaceRequirement && fx.Res1 != null && fx.Res1 == fx.Res2)
        {
            Err(DataErrorCode.DuplicateReference, JsonPath.Prop(path, "to"), owner, "duplicate reference '" + fx.Res2 + "'");
        }

        t.Effects.Add(fx);
    }

    private static bool IsOptionalPair(TechEffectType type, int slot) =>
        (type == TechEffectType.ModifyStat && slot >= 2) || (type == TechEffectType.Obsolete);

    private static int Count(bool[] seen, int a, int b) => (seen[a] ? 1 : 0) + (seen[b] ? 1 : 0);

    // ---------- etapa 3: referências ----------

    private void Stage3()
    {
        _file = DataFiles.Resources;
        for (var i = 0; i < _res.Count; i++)
        {
            var r = _res[i];
            if (r == null)
            {
                continue;
            }

            var path = JsonPath.Index("$.resources", i);
            if (r.RevealedBy != null && !_techMap.ContainsKey(r.RevealedBy))
            {
                Err(DataErrorCode.UnknownTech, JsonPath.Prop(path, "revealedBy"), r.Id, "unknown tech '" + r.RevealedBy + "'");
            }

            if (r.ExtractedBy != null)
            {
                Pending(ContentKind.Building, r.ExtractedBy, DataFiles.Resources, JsonPath.Prop(path, "extractedBy"), r.Id);
            }
        }

        _file = DataFiles.Techs;
        for (var i = 0; i < _techs.Count; i++)
        {
            var t = _techs[i];
            if (t == null)
            {
                continue;
            }

            var path = JsonPath.Index("$.techs", i);
            foreach (var c in t.Cost)
            {
                if (!_resMap.ContainsKey(c.Key))
                {
                    Err(DataErrorCode.UnknownResource, JsonPath.Prop(JsonPath.Prop(path, "cost"), c.Key), t.Id, "unknown resource '" + c.Key + "'");
                }
            }

            foreach (var q in t.Requires)
            {
                if (!_techMap.ContainsKey(q.Key))
                {
                    Err(DataErrorCode.UnknownTech, JsonPath.Index(JsonPath.Prop(path, "requires"), q.Index), t.Id, "unknown tech '" + q.Key + "'");
                }
            }

            foreach (var fx in t.Effects)
            {
                var epath = JsonPath.Index(JsonPath.Prop(path, "effects"), fx.Index);
                CheckResource(fx.Res1, JsonPath.Prop(epath, fx.Res1Prop), t.Id);
                CheckResource(fx.Res2, JsonPath.Prop(epath, "to"), t.Id);
                if (fx.Target != null && fx.Type != TechEffectType.RevealResource)
                {
                    Pending(fx.Kind, fx.Target, DataFiles.Techs, JsonPath.Prop(epath, fx.TargetProp), t.Id);
                }
            }
        }
    }

    private void CheckResource(string? key, string path, string? owner)
    {
        if (key != null && !_resMap.ContainsKey(key))
        {
            Err(DataErrorCode.UnknownResource, path, owner, "unknown resource '" + key + "'");
        }
    }

    private void Pending(ContentKind kind, string target, string file, string path, string? owner)
    {
        _pending.Add(new PendingReference(kind, target, file, path, owner ?? ""));
        var name = kind == ContentKind.Building ? "building" : kind == ContentKind.Unit ? "unit" : "recipe";
        Warn(file, path, owner, name + " '" + target + "' is not validated yet");
    }

    // ---------- etapa 4: coerência revealedBy <=> revealResource ----------

    private static bool HasReveal(TechEntry t, string resourceKey)
    {
        foreach (var fx in t.Effects)
        {
            if (fx.Type == TechEffectType.RevealResource && fx.Res1 == resourceKey)
            {
                return true;
            }
        }

        return false;
    }

    private void Stage4()
    {
        _file = DataFiles.Resources;
        for (var i = 0; i < _res.Count; i++)
        {
            var r = _res[i];
            if (r?.RevealedBy == null || r.Id == null || !_techMap.TryGetValue(r.RevealedBy, out var ti))
            {
                continue;
            }

            if (!HasReveal(_techs[ti]!, r.Id))
            {
                Err(DataErrorCode.MissingRevealEffect, JsonPath.Prop(JsonPath.Index("$.resources", i), "revealedBy"), r.Id,
                    "tech '" + r.RevealedBy + "' has no revealResource effect for '" + r.Id + "'");
            }
        }

        _file = DataFiles.Techs;
        for (var i = 0; i < _techs.Count; i++)
        {
            var t = _techs[i];
            if (t == null)
            {
                continue;
            }

            foreach (var fx in t.Effects)
            {
                if (fx.Type != TechEffectType.RevealResource || fx.Res1 == null || !_resMap.TryGetValue(fx.Res1, out var ri))
                {
                    continue;
                }

                if (_res[ri]!.RevealedBy != t.Id)
                {
                    var epath = JsonPath.Index(JsonPath.Prop(JsonPath.Index("$.techs", i), "effects"), fx.Index);
                    Err(DataErrorCode.RevealEffectMismatch, epath, t.Id, "resource '" + fx.Res1 + "' is not revealedBy '" + t.Id + "'");
                }
            }
        }
    }

    // ---------- etapa 5: ciclos ----------

    private void Stage5()
    {
        _file = DataFiles.Techs;
        var color = new byte[_techs.Count]; // 0 branco, 1 cinza, 2 preto
        var stack = new List<int>();
        for (var i = 0; i < _techs.Count; i++)
        {
            if (_techs[i] != null && color[i] == 0)
            {
                Visit(i, color, stack);
            }
        }
    }

    private void Visit(int u, byte[] color, List<int> stack)
    {
        color[u] = 1;
        stack.Add(u);
        var tu = _techs[u]!;
        foreach (var q in tu.Requires)
        {
            if (!_techMap.TryGetValue(q.Key, out var v))
            {
                continue;
            }

            if (color[v] == 1)
            {
                var sb = new StringBuilder("cycle: ");
                var start = stack.IndexOf(v);
                for (var k = start; k < stack.Count; k++)
                {
                    sb.Append(_techs[stack[k]]!.Id).Append(" → ");
                }

                sb.Append(_techs[v]!.Id);
                Err(DataErrorCode.TechCycle, JsonPath.Index(JsonPath.Prop(JsonPath.Index("$.techs", u), "requires"), q.Index), tu.Id, sb.ToString());
            }
            else if (color[v] == 0)
            {
                Visit(v, color, stack);
            }
        }

        stack.RemoveAt(stack.Count - 1);
        color[u] = 2;
    }

    // ---------- montagem (RN-P3-60) ----------

    private DataSet Assemble()
    {
        var resDefs = new ResourceDef[_res.Count];
        for (var i = 0; i < resDefs.Length; i++)
        {
            var r = _res[i]!;
            TechId? revealed = r.RevealedBy != null ? new TechId(_techMap[r.RevealedBy]) : null;
            resDefs[i] = new ResourceDef(new ResourceId(i), r.Id!, r.Category, r.Storage, r.Renewable, r.Depletes,
                revealed, r.ExtractedBy, r.Quota);
        }

        var techDefs = new TechDef[_techs.Count];
        for (var i = 0; i < techDefs.Length; i++)
        {
            var t = _techs[i]!;
            var cost = new ResourceAmount[t.Cost.Count];
            for (var k = 0; k < cost.Length; k++)
            {
                cost[k] = new ResourceAmount(new ResourceId(_resMap[t.Cost[k].Key]), Fixed.FromInt(t.Cost[k].Value));
            }

            Array.Sort(cost, static (a, b) => a.Resource.Value.CompareTo(b.Resource.Value));

            var requires = new TechId[t.Requires.Count];
            for (var k = 0; k < requires.Length; k++)
            {
                requires[k] = new TechId(_techMap[t.Requires[k].Key]);
            }

            Array.Sort(requires, static (a, b) => a.Value.CompareTo(b.Value));

            var effects = new TechEffect[t.Effects.Count];
            for (var k = 0; k < effects.Length; k++)
            {
                effects[k] = BuildEffect(t.Effects[k]);
            }

            techDefs[i] = new TechDef(new TechId(i), t.Id!, t.Tier, cost, t.Ticks, requires, effects);
        }

        var warnings = new List<DataError>();
        foreach (var item in _items)
        {
            if (item.Severity == DataSeverity.Warning)
            {
                warnings.Add(item);
            }
        }

        return new DataSet(new ResourceRegistry(resDefs), new TechRegistry(techDefs), warnings.ToArray(), _pending.ToArray());
    }

    private TechEffect BuildEffect(EffectEntry fx) => fx.Type switch
    {
        TechEffectType.RevealResource => new RevealResourceEffect(new ResourceId(_resMap[fx.Res1!])),
        TechEffectType.UnlockBuilding => new UnlockBuildingEffect(fx.Target!),
        TechEffectType.UnlockUnit => new UnlockUnitEffect(fx.Target!),
        TechEffectType.UnlockRecipe => new UnlockRecipeEffect(fx.Target!),
        TechEffectType.ModifyStat => new ModifyStatEffect(fx.Kind, fx.Target!, fx.Stat!, fx.Mod, fx.Value),
        TechEffectType.RaiseGatherCap => new RaiseGatherCapEffect(new ResourceId(_resMap[fx.Res1!]), fx.Value),
        TechEffectType.ReplaceRequirement => new ReplaceRequirementEffect(new ResourceId(_resMap[fx.Res1!]), new ResourceId(_resMap[fx.Res2!])),
        _ => new ObsoleteEffect(fx.Kind, fx.Target!),
    };

    // ---------- etapa 6: alcançabilidade (RN-P3-50) ----------

    private void Stage6(DataSet set)
    {
        var rc = set.Resources.Count;
        var tc = set.Techs.Count;
        var reachable = new bool[rc];
        var researchable = new bool[tc];
        for (var i = 0; i < rc; i++)
        {
            reachable[i] = set.Resources.All[i].RevealedBy == null;
        }

        bool changed;
        do
        {
            changed = false;
            for (var i = 0; i < tc; i++)
            {
                if (researchable[i])
                {
                    continue;
                }

                var t = set.Techs.All[i];
                var ok = true;
                foreach (var q in t.Requires)
                {
                    ok = ok && researchable[q.Value];
                }

                foreach (var c in t.Cost)
                {
                    ok = ok && reachable[c.Resource.Value];
                }

                if (!ok)
                {
                    continue;
                }

                researchable[i] = true;
                changed = true;
                foreach (var e in t.Effects)
                {
                    if (e is RevealResourceEffect rv)
                    {
                        reachable[rv.Resource.Value] = true;
                    }
                }
            }
        }
        while (changed);

        _file = DataFiles.Resources;
        for (var i = 0; i < rc; i++)
        {
            if (!reachable[i])
            {
                var r = set.Resources.All[i];
                Err(DataErrorCode.UnreachableResource, JsonPath.Index("$.resources", i), r.Key,
                    "resource is revealed by unresearchable tech '" + set.Techs[r.RevealedBy!.Value].Key + "'");
            }
        }

        _file = DataFiles.Techs;
        for (var i = 0; i < tc; i++)
        {
            if (researchable[i])
            {
                continue;
            }

            var t = set.Techs.All[i];
            string? reason = null;
            foreach (var q in _techs[i]!.Requires)
            {
                var qi = _techMap[q.Key];
                if (!researchable[qi])
                {
                    reason = "requires unresearchable tech '" + q.Key + "'";
                    break;
                }
            }

            if (reason == null)
            {
                foreach (var c in t.Cost)
                {
                    if (!reachable[c.Resource.Value])
                    {
                        reason = "cost uses unreachable resource '" + set.Resources[c.Resource].Key + "'";
                        break;
                    }
                }
            }

            Err(DataErrorCode.UnresearchableTech, JsonPath.Index("$.techs", i), t.Key, "tech cannot be researched: " + reason);
        }
    }
}
