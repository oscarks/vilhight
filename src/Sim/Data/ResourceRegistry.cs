using System;
using System.Collections.Generic;
using Rts.Sim.State;

namespace Rts.Sim.Data;

public sealed class ResourceRegistry
{
    private readonly ResourceDef[] _defs;
    private readonly IReadOnlyList<ResourceDef> _all;
    private readonly Dictionary<string, ResourceId> _byKey; // só busca, nunca iterado

    internal ResourceRegistry(ResourceDef[] defs)
    {
        _defs = defs;
        _all = Array.AsReadOnly(defs);
        _byKey = new Dictionary<string, ResourceId>(defs.Length, StringComparer.Ordinal);
        foreach (var d in defs)
        {
            _byKey[d.Key] = d.Id;
        }
    }

    public int Count => _defs.Length;

    public IReadOnlyList<ResourceDef> All => _all;

    public ResourceDef this[ResourceId id]
    {
        get
        {
            if (id.Value < 0 || id.Value >= _defs.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            return _defs[id.Value];
        }
    }

    public bool TryFind(string key, out ResourceId id)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _byKey.TryGetValue(key, out id);
    }
}
