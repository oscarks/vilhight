using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Data;

// Etapas 3 e 4: referências e coerência revealedBy/revealResource.
internal sealed partial class DataCompiler
{
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
}
