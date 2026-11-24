using Rts.Sim.State;

namespace Rts.Sim.Data;

// design: Árvore de tecnologia > Efeitos declarativos. Um handler por tipo virá no E8-2.
public enum TechEffectType : byte
{
    RevealResource = 1,
    UnlockBuilding = 2,
    UnlockUnit = 3,
    UnlockRecipe = 4,
    ModifyStat = 5,
    RaiseGatherCap = 6,
    ReplaceRequirement = 7,
    Obsolete = 8,
}

public enum StatModifierKind : byte { Add = 1, Percent = 2 }

public abstract record TechEffect
{
    private protected TechEffect()
    {
    }

    public abstract TechEffectType Type { get; }
}

public sealed record RevealResourceEffect(ResourceId Resource) : TechEffect
{
    public override TechEffectType Type => TechEffectType.RevealResource;
}

public sealed record UnlockBuildingEffect(string Building) : TechEffect
{
    public override TechEffectType Type => TechEffectType.UnlockBuilding;
}

public sealed record UnlockUnitEffect(string Unit) : TechEffect
{
    public override TechEffectType Type => TechEffectType.UnlockUnit;
}

public sealed record UnlockRecipeEffect(string Recipe) : TechEffect
{
    public override TechEffectType Type => TechEffectType.UnlockRecipe;
}

// TargetKind ∈ {Unit, Building}; Value != 0. Esquema provisório (DD-P3-04).
public sealed record ModifyStatEffect(ContentKind TargetKind, string Target, string Stat,
    StatModifierKind Modifier, int Value) : TechEffect
{
    public override TechEffectType Type => TechEffectType.ModifyStat;
}

public sealed record RaiseGatherCapEffect(ResourceId Resource, int Percent) : TechEffect
{
    public override TechEffectType Type => TechEffectType.RaiseGatherCap;
}

public sealed record ReplaceRequirementEffect(ResourceId From, ResourceId To) : TechEffect
{
    public override TechEffectType Type => TechEffectType.ReplaceRequirement;
}

// TargetKind ∈ {Unit, Recipe}.
public sealed record ObsoleteEffect(ContentKind TargetKind, string Target) : TechEffect
{
    public override TechEffectType Type => TechEffectType.Obsolete;
}
