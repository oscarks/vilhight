using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Data;

// Montagem do DataSet (RN-P3-60).
internal sealed partial class DataCompiler
{
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
}
