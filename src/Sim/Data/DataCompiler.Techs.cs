using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Data;

// Etapa 2 (techs): custo e requisitos.
internal sealed partial class DataCompiler
{
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
}
