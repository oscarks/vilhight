using System;
using System.Collections.Generic;
using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Data;

public readonly record struct ResourceAmount(ResourceId Resource, Fixed Amount);

public sealed class TechDef
{
    internal TechDef(TechId id, string key, int tier, ResourceAmount[] cost, int researchTicks,
        TechId[] requires, TechEffect[] effects)
    {
        Id = id;
        Key = key;
        Tier = tier;
        ResearchTicks = researchTicks;
        Cost = Array.AsReadOnly(cost);
        Requires = Array.AsReadOnly(requires);
        Effects = Array.AsReadOnly(effects);
    }

    public TechId Id { get; }
    public string Key { get; }
    public int Tier { get; }
    public IReadOnlyList<ResourceAmount> Cost { get; }
    public int ResearchTicks { get; }
    public IReadOnlyList<TechId> Requires { get; }
    public IReadOnlyList<TechEffect> Effects { get; }
}
