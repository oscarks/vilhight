using System.Text.Json.Serialization;
using Rts.Sim.Commands;
using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Tests.Fixtures;

public sealed record AddStockCommand(
    CountryId Country,
    ControllerRef Issuer,
    [property: JsonPropertyOrder(1)] ResourceId Resource,
    [property: JsonPropertyOrder(2)] Fixed Amount) : Command(Country, Issuer);

public sealed record RollCommand(
    CountryId Country,
    ControllerRef Issuer,
    [property: JsonPropertyOrder(1)] ResourceId Resource,
    [property: JsonPropertyOrder(2)] int Max) : Command(Country, Issuer);

public static class TestCommands
{
    public const string AddStock = "test.addStock";
    public const string Roll = "test.roll";

    public static CommandRegistry CreateRegistry()
    {
        var registry = new CommandRegistry();
        registry.Register<AddStockCommand>(AddStock, HandleAddStock);
        registry.Register<RollCommand>(Roll, HandleRoll);
        return registry;
    }

    public static CommandRejectReason HandleAddStock(GameState state, SimContext context, AddStockCommand cmd)
    {
        if (cmd.Resource.Value < 0 || cmd.Resource.Value >= state.ResourceCount)
        {
            return CommandRejectReason.InvalidArguments;
        }

        var country = state.FindCountry(cmd.Country)!;
        country.SetStock(cmd.Resource, country.GetStock(cmd.Resource) + cmd.Amount);
        return CommandRejectReason.None;
    }

    public static CommandRejectReason HandleRoll(GameState state, SimContext context, RollCommand cmd)
    {
        if (cmd.Max <= 0 || cmd.Resource.Value < 0 || cmd.Resource.Value >= state.ResourceCount)
        {
            return CommandRejectReason.InvalidArguments;
        }

        var country = state.FindCountry(cmd.Country)!;
        var v = state.Random.NextInt(cmd.Max);
        country.SetStock(cmd.Resource, country.GetStock(cmd.Resource) + Fixed.FromInt(v));
        return CommandRejectReason.None;
    }
}
