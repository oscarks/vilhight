using System;
using Rts.Sim.Core;

namespace Rts.Sim.State;

public sealed class Country
{
    private readonly Fixed[] _stock;

    internal Country(CountryId id, WorldId world, string name, ControllerRef controller, int resourceCount)
    {
        Id = id;
        World = world;
        Name = name;
        Controller = controller;
        _stock = new Fixed[resourceCount];
    }

    public CountryId Id { get; }
    public WorldId World { get; }
    public string Name { get; }
    public ControllerRef Controller { get; internal set; }
    public ReadOnlySpan<Fixed> Stock => _stock;

    public Fixed GetStock(ResourceId resource)
    {
        if ((uint)resource.Value >= (uint)_stock.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(resource));
        }

        return _stock[resource.Value];
    }

    internal void SetStock(ResourceId resource, Fixed amount)
    {
        if ((uint)resource.Value >= (uint)_stock.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(resource));
        }

        _stock[resource.Value] = amount;
    }
}
