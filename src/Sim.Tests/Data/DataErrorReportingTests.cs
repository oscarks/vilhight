using System;
using Rts.Sim.Data;
using Xunit;
using static Rts.Sim.Tests.Data.D;
using static Rts.Sim.Tests.Fixtures.DataFixtures;

namespace Rts.Sim.Tests.Data;

public sealed class DataErrorReportingTests
{
    [Fact]
    public void Parse_ThrowsWithAllErrors()
    {
        // UT-BE-133
        var raw = Raw(
            Resources(
                """{"id":"r_a","category":"basic","storage":"stock"}""",
                """{"id":"r_a","category":"basic","storage":"stock"}"""),
            Techs("""{"id":"t_a","tier":1,"cost":{"r_a":1},"researchTicks":1,"requires":["t_zz"],"effects":[{"type":"teleport"}]}"""));
        var ex = Assert.Throws<DataValidationException>(() => DataLoader.Parse(raw));
        Assert.Equal(3, ex.Errors.Count);
        var seq = new (DataErrorCode, string, string, string?)[ex.Errors.Count];
        for (var i = 0; i < seq.Length; i++)
        {
            seq[i] = (ex.Errors[i].Code, ex.Errors[i].File, ex.Errors[i].Path, ex.Errors[i].Id);
        }

        Assert.Equal(
            new[]
            {
                E(DataErrorCode.DuplicateId, R, "$.resources[1].id", "r_a"),
                E(DataErrorCode.UnknownEffectType, T, "$.techs[0].effects[0].type", "t_a"),
                E(DataErrorCode.UnknownTech, T, "$.techs[0].requires[0]", "t_a"),
            },
            seq);
        Assert.Equal(
            "data validation failed with 3 error(s):\n" +
            "error resources.json $.resources[1].id id=r_a DuplicateId: duplicate id 'r_a' (first at index 0)\n" +
            "error techs.json $.techs[0].effects[0].type id=t_a UnknownEffectType: unknown effect type 'teleport'\n" +
            "error techs.json $.techs[0].requires[0] id=t_a UnknownTech: unknown tech 't_zz'",
            ex.Message);
        Assert.Empty(ex.Warnings);
        Expect(
            raw,
            E(DataErrorCode.DuplicateId, R, "$.resources[1].id", "r_a"),
            E(DataErrorCode.UnknownEffectType, T, "$.techs[0].effects[0].type", "t_a"),
            E(DataErrorCode.UnknownTech, T, "$.techs[0].requires[0]", "t_a"));
        DataLoader.Parse(Base());
        Assert.Throws<ArgumentNullException>(() => DataLoader.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => DataValidator.Validate(null!));
    }

    [Fact]
    public void DataError_ToString_UsesFixedFormat()
    {
        // UT-BE-134
        var e = new DataError(DataSeverity.Error, DataErrorCode.InvalidId, "techs.json", "$.techs[0].id", null, "invalid id 'X'");
        Assert.Equal("error techs.json $.techs[0].id id=- InvalidId: invalid id 'X'", e.ToString());
        Assert.StartsWith("warning ", (e with { Severity = DataSeverity.Warning }).ToString());
    }
}
