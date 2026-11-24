using System;
using System.Collections.Generic;
using Rts.Sim.State;

namespace Rts.Sim.Data;

public sealed class TechRegistry
{
    private readonly TechDef[] _defs;
    private readonly IReadOnlyList<TechDef> _all;
    private readonly Dictionary<string, TechId> _byKey; // só busca, nunca iterado

    internal TechRegistry(TechDef[] defs)
    {
        _defs = defs;
        _all = Array.AsReadOnly(defs);
        _byKey = new Dictionary<string, TechId>(defs.Length, StringComparer.Ordinal);
        foreach (var d in defs)
        {
            _byKey[d.Key] = d.Id;
        }
    }

    public int Count => _defs.Length;

    public IReadOnlyList<TechDef> All => _all;

    public TechDef this[TechId id]
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

    public bool TryFind(string key, out TechId id)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _byKey.TryGetValue(key, out id);
    }
}
