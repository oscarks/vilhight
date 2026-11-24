using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Data;

// Etapas 1 e 2 (recursos): leitura estrutural do JSON.
internal sealed partial class DataCompiler
{
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
}
