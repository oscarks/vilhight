using Rts.Sim.Data;
using Xunit;
using static Rts.Sim.Tests.Data.D;
using static Rts.Sim.Tests.Fixtures.DataFixtures;

namespace Rts.Sim.Tests.Data;

public sealed class DataReferenceTests
{
    [Fact]
    public void UnknownReferences_AreReported()
    {
        // UT-BE-128
        ExpectTech(T1(requires: "[\"t_zz\"]"), E(DataErrorCode.UnknownTech, T, "$.techs[0].requires[0]", "t_a"));
        ExpectTech(T1(cost: """{"r_zz":5}"""), E(DataErrorCode.UnknownResource, T, "$.techs[0].cost.r_zz", "t_a"));
        Expect(
            Raw(Resources("""{"id":"r_h","category":"raw_hidden","storage":"stock","revealedBy":"t_zz"}"""), NoTechs),
            E(DataErrorCode.UnknownTech, R, "$.resources[0].revealedBy", "r_h"));
        ExpectEffect("""{"type":"revealResource","resource":"r_zz"}""", E(DataErrorCode.UnknownResource, T, "$.techs[0].effects[0].resource", "t_a"));
        ExpectEffect("""{"type":"replaceRequirement","from":"r_a","to":"r_zz"}""", E(DataErrorCode.UnknownResource, T, "$.techs[0].effects[0].to", "t_a"));
        Expect(
            Raw(Resources("""{"id":"r_a","category":"gas","storage":"stock"}"""), Techs(T1(cost: """{"r_a":1}"""))),
            E(DataErrorCode.InvalidEnumValue, R, "$.resources[0].category", "r_a"));
    }

    [Fact]
    public void RevealConsistency_IsEnforced()
    {
        // UT-BE-129
        const string hidden = """{"id":"r_h","category":"raw_hidden","storage":"stock","revealedBy":"t_a"}""";
        var c1 = Raw(Resources(hidden), Techs(T1()));
        Expect(c1, E(DataErrorCode.MissingRevealEffect, R, "$.resources[0].revealedBy", "r_h"));
        Assert.Equal("tech 't_a' has no revealResource effect for 'r_h'", MessageOf(c1, DataErrorCode.MissingRevealEffect));
        var c2 = Raw(OneResource, Techs(T1(effects: """[{"type":"revealResource","resource":"r_a"}]""")));
        Expect(c2, E(DataErrorCode.RevealEffectMismatch, T, "$.techs[0].effects[0]", "t_a"));
        Assert.Equal("resource 'r_a' is not revealedBy 't_a'", MessageOf(c2, DataErrorCode.RevealEffectMismatch));
        Expect(
            Raw(
                Resources("""{"id":"r_h","category":"raw_hidden","storage":"stock","revealedBy":"t_b"}"""),
                Techs(T1(effects: """[{"type":"revealResource","resource":"r_h"}]"""), T1(id: "t_b"))),
            E(DataErrorCode.MissingRevealEffect, R, "$.resources[0].revealedBy", "r_h"),
            E(DataErrorCode.RevealEffectMismatch, T, "$.techs[0].effects[0]", "t_a"));
    }

    [Fact]
    public void Cycles_ReportThePath()
    {
        // UT-BE-130
        var c1 = Raw(OneResource, Techs(T1(requires: "[\"t_b\"]"), T1(id: "t_b", requires: "[\"t_a\"]")));
        Expect(c1, E(DataErrorCode.TechCycle, T, "$.techs[1].requires[0]", "t_b"));
        Assert.Equal("cycle: t_a → t_b → t_a", MessageOf(c1, DataErrorCode.TechCycle));
        var c2 = Raw(OneResource, Techs(T1(requires: "[\"t_a\"]")));
        Expect(c2, E(DataErrorCode.TechCycle, T, "$.techs[0].requires[0]", "t_a"));
        Assert.Equal("cycle: t_a → t_a", MessageOf(c2, DataErrorCode.TechCycle));
        var c3 = Raw(OneResource, Techs(T1(requires: "[\"t_b\"]"), T1(id: "t_b", requires: "[\"t_c\"]"), T1(id: "t_c", requires: "[\"t_a\"]")));
        Expect(c3, E(DataErrorCode.TechCycle, T, "$.techs[2].requires[0]", "t_c"));
        Assert.Equal("cycle: t_a → t_b → t_c → t_a", MessageOf(c3, DataErrorCode.TechCycle));
        Expect(Raw(OneResource, Techs(
            T1(id: "t_d", requires: "[\"t_b\",\"t_c\"]"),
            T1(id: "t_b", requires: "[\"t_a\"]"),
            T1(id: "t_c", requires: "[\"t_a\"]"),
            T1())));
        Expect(
            Raw("{", Techs(T1(requires: "[\"t_b\"]"), T1(id: "t_b", requires: "[\"t_a\"]"))),
            E(DataErrorCode.InvalidJson, R, "$", null),
            E(DataErrorCode.TechCycle, T, "$.techs[1].requires[0]", "t_b"));
    }

    [Fact]
    public void Reachability_UsesFixedPoint()
    {
        // UT-BE-131
        const string hidden = """{"id":"r_h","category":"raw_hidden","storage":"stock","revealedBy":"t_a"}""";
        const string reveal = """[{"type":"revealResource","resource":"r_h"}]""";
        var c1 = Raw(Resources(hidden), Techs(T1(cost: """{"r_h":1}""", effects: reveal)));
        Expect(
            c1,
            E(DataErrorCode.UnreachableResource, R, "$.resources[0]", "r_h"),
            E(DataErrorCode.UnresearchableTech, T, "$.techs[0]", "t_a"));
        Assert.Equal("resource is revealed by unresearchable tech 't_a'", MessageOf(c1, DataErrorCode.UnreachableResource));
        Assert.Equal("tech cannot be researched: cost uses unreachable resource 'r_h'", MessageOf(c1, DataErrorCode.UnresearchableTech));
        var c2 = Raw(Resources(hidden), Techs(T1(cost: """{"r_h":1}""", effects: reveal), T1(id: "t_b", requires: "[\"t_a\"]")));
        Expect(
            c2,
            E(DataErrorCode.UnreachableResource, R, "$.resources[0]", "r_h"),
            E(DataErrorCode.UnresearchableTech, T, "$.techs[0]", "t_a"),
            E(DataErrorCode.UnresearchableTech, T, "$.techs[1]", "t_b"));
        var msgs = new System.Collections.Generic.List<string>();
        foreach (var e in V(c2))
        {
            if (e.Code == DataErrorCode.UnresearchableTech)
            {
                msgs.Add(e.Message);
            }
        }

        Assert.Equal("tech cannot be researched: requires unresearchable tech 't_a'", msgs[1]);
        Expect(Raw(Resources(hidden), Techs(
            T1(id: "t_b", cost: """{"r_h":1}""", requires: "[\"t_a\"]"),
            T1(effects: reveal))));
        Expect(Base());
    }

    [Fact]
    public void StageSuppression_AvoidsCascades()
    {
        // UT-BE-132
        Expect(Raw("{", Techs(T1(cost: """{"r_zz":1}"""))), E(DataErrorCode.InvalidJson, R, "$", null));
        Expect(Raw(null, Techs(T1(requires: "[\"t_zz\"]"))), E(DataErrorCode.FileMissing, R, "$", null));
        const string hidden = """{"id":"r_h","category":"raw_hidden","storage":"stock","revealedBy":"t_a"}""";
        Expect(
            Raw(Resources(hidden), Techs(
                T1(requires: "[\"t_b\"]", effects: """[{"type":"revealResource","resource":"r_h"}]"""),
                T1(id: "t_b", requires: "[\"t_a\"]"))),
            E(DataErrorCode.TechCycle, T, "$.techs[1].requires[0]", "t_b"));
        Expect(
            Raw(
                Resources("""{"id":"r_a","category":"gas","storage":"stock"}""", hidden.Replace("r_h", "r_h")),
                Techs(T1(cost: """{"r_h":1}""", effects: """[{"type":"revealResource","resource":"r_h"}]"""))),
            E(DataErrorCode.InvalidEnumValue, R, "$.resources[0].category", "r_a"));
    }
}
