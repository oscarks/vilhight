using Rts.Sim.State;

namespace Rts.Sim.Data;

public enum ResourceCategory : byte
{
    Basic = 1,
    RawHidden = 2,
    Refined = 3,
    Strategic = 4,
    Abstract = 5,
}

public enum ResourceStorage : byte
{
    Stock = 1,
    Quota = 2,
}

public sealed class ResourceDef
{
    internal ResourceDef(ResourceId id, string key, ResourceCategory category, ResourceStorage storage,
        bool renewable, bool depletes, TechId? revealedBy, string? extractedBy, int quotaPerSource)
    {
        Id = id;
        Key = key;
        Category = category;
        Storage = storage;
        Renewable = renewable;
        Depletes = depletes;
        RevealedBy = revealedBy;
        ExtractedBy = extractedBy;
        QuotaPerSource = quotaPerSource;
    }

    public ResourceId Id { get; }
    public string Key { get; }
    public ResourceCategory Category { get; }
    public ResourceStorage Storage { get; }
    public bool Renewable { get; }
    public bool Depletes { get; }
    public TechId? RevealedBy { get; }
    public string? ExtractedBy { get; }
    public int QuotaPerSource { get; }
}
