using System.Collections.Generic;
using Rts.Sim.Data;
using Rts.Sim.Tests.Fixtures;
using Xunit;

namespace Rts.Sim.Tests.Data;

internal static class D
{
    public const string R = DataFiles.Resources;
    public const string T = DataFiles.Techs;

    public static (DataErrorCode, string, string, string?) E(DataErrorCode code, string file, string path, string? id) =>
        (code, file, path, id);

    public static void Expect(RawData raw, params (DataErrorCode, string, string, string?)[] expected)
    {
        var actual = DataFixtures.Errs(raw);
        Assert.Equal(expected.Length, actual.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    public static void ExpectRes(string item, params (DataErrorCode, string, string, string?)[] expected) =>
        Expect(DataFixtures.Raw(DataFixtures.Resources(item), DataFixtures.NoTechs), expected);

    public static void ExpectTech(string item, params (DataErrorCode, string, string, string?)[] expected) =>
        Expect(DataFixtures.Raw(DataFixtures.OneResource, DataFixtures.Techs(item)), expected);

    public static void ExpectEffect(string effect, params (DataErrorCode, string, string, string?)[] expected) =>
        ExpectTech(DataFixtures.T1(effects: "[" + effect + "]"), expected);

    public static string? MessageOf(RawData raw, DataErrorCode code)
    {
        foreach (var e in DataValidator.Validate(raw))
        {
            if (e.Code == code)
            {
                return e.Message;
            }
        }

        return null;
    }

    public static IReadOnlyList<DataError> V(RawData raw) => DataValidator.Validate(raw);
}
