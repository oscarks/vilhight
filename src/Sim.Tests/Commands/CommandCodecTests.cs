using System;
using Rts.Sim.Commands;
using Rts.Sim.Core;
using Rts.Sim.State;
using Rts.Sim.Tests.Fixtures;
using Xunit;

namespace Rts.Sim.Tests.Commands;

public sealed class CommandCodecTests
{
    private const string AddStockJson =
        "{\"type\":\"test.addStock\",\"country\":1,\"issuer\":{\"kind\":1,\"player\":1},\"resource\":2,\"amount\":30064771072}";

    private const string RollJson =
        "{\"type\":\"test.roll\",\"country\":3,\"issuer\":{\"kind\":2,\"player\":1},\"resource\":0,\"max\":6}";

    private static CommandCodec NewCodec() => new(TestCommands.CreateRegistry());

    [Fact]
    public void Serialize_ExactFormat()
    {
        // UT-BE-73
        var codec = NewCodec();
        var a = Refs.AddStock(1, Refs.Human1, 2, Fixed.FromInt(7));
        Assert.Equal(AddStockJson, codec.Serialize(a));
        Assert.Equal(RollJson, codec.Serialize(Refs.Roll(3, Refs.Ai1, 0, 6)));
        Assert.Contains("\"amount\":-1", codec.Serialize(Refs.AddStock(1, Refs.Human1, 0, Fixed.FromRaw(-1))));
        Assert.Equal(codec.Serialize(a), codec.Serialize(a));
    }

    [Fact]
    public void Codec_RoundTrip()
    {
        // UT-BE-74
        var codec = NewCodec();
        var total = 0;
        foreach (var list in DeterminismScenario.BuildScript())
        {
            foreach (var c in list)
            {
                var back = codec.Deserialize(codec.Serialize(c));
                Assert.Equal(c, back);
                Assert.Equal(c.GetType(), back.GetType());
                total++;
            }
        }

        Assert.Equal(1515, total);

        foreach (var amount in new[] { Fixed.MinValue, Fixed.MaxValue })
        {
            var c = Refs.AddStock(1, Refs.Human1, 0, amount);
            Assert.Equal(c, codec.Deserialize(codec.Serialize(c)));
        }

        Assert.Equal(AddStockJson, codec.Serialize(codec.Deserialize(AddStockJson)));
        Assert.Equal(RollJson, codec.Serialize(codec.Deserialize(RollJson)));
    }

    [Theory]
    [InlineData("{\"type\":\"nope\",\"country\":1,\"issuer\":{\"kind\":1,\"player\":1}}")]
    [InlineData("{\"country\":1,\"issuer\":{\"kind\":1,\"player\":1},\"resource\":2,\"amount\":5}")]
    [InlineData("{\"country\":1,\"type\":\"test.addStock\",\"issuer\":{\"kind\":1,\"player\":1},\"resource\":2,\"amount\":5}")]
    [InlineData("{\"type\":\"test.addStock\",\"issuer\":{\"kind\":1,\"player\":1},\"resource\":2,\"amount\":5}")]
    [InlineData("{\"type\":\"test.addStock\",\"country\":1,\"issuer\":{\"kind\":1},\"resource\":2,\"amount\":5}")]
    [InlineData("{\"type\":\"test.addStock\",\"country\":1,\"issuer\":{\"kind\":1,\"player\":1},\"resource\":2,\"amount\":5,\"x\":1}")]
    [InlineData("{\"type\":\"test.addStock\",\"country\":1,\"issuer\":{\"kind\":1,\"player\":1},\"resource\":2,\"amount\":5.5}")]
    [InlineData("{\"type\":\"test.addStock\",\"country\":1,\"issuer\":{\"kind\":1,\"player\":1},\"resource\":2,\"amount\":\"5\"}")]
    [InlineData("{\"type\":\"test.addStock\",\"Country\":1,\"issuer\":{\"kind\":1,\"player\":1},\"resource\":2,\"amount\":5}")]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("")]
    public void Deserialize_RejectsInvalid(string json)
    {
        // UT-BE-75
        Assert.Throws<FormatException>(() => NewCodec().Deserialize(json));
    }

    [Fact]
    public void Deserialize_NullAndInnerException()
    {
        // UT-BE-75 (asserts adicionais)
        var codec = NewCodec();
        Assert.Throws<ArgumentNullException>(() => codec.Deserialize(null!));
        var ex = Assert.Throws<FormatException>(() =>
            codec.Deserialize("{\"type\":\"nope\",\"country\":1,\"issuer\":{\"kind\":1,\"player\":1}}"));
        Assert.IsAssignableFrom<System.Text.Json.JsonException>(ex.InnerException);
    }

    [Fact]
    public void Serialize_UnregisteredType()
    {
        // UT-BE-76
        var registry = new CommandRegistry();
        registry.Register<AddStockCommand>(TestCommands.AddStock, TestCommands.HandleAddStock);
        var codec = new CommandCodec(registry);
        Assert.Throws<ArgumentException>(() => codec.Serialize(Refs.Roll(1, Refs.Human1, 0, 6)));
        Assert.Throws<ArgumentNullException>(() => codec.Serialize(null!));
    }
}
