using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Data;

// Etapa 6: alcançabilidade.
internal sealed partial class DataCompiler
{
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
