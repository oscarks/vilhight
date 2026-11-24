using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Data;

// Etapa 2 (techs): efeitos declarativos.
internal sealed partial class DataCompiler
{
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
}
