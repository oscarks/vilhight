using System;
using Rts.Sim.Data;
using Rts.Sim.Tests.Fixtures;
using Xunit;
using static Rts.Sim.Tests.Data.D;
using static Rts.Sim.Tests.Fixtures.DataFixtures;

namespace Rts.Sim.Tests.Data;

public sealed class DataStructureTests
{
    [Fact]
    public void FileAndRootErrors_AreReported()
    {
        // UT-BE-124
        Expect(Raw(null, NoTechs), E(DataErrorCode.FileMissing, R, "$", null));
        Expect(Raw(OneResource, null), E(DataErrorCode.FileMissing, T, "$", null));
        Expect(Raw("{", NoTechs), E(DataErrorCode.InvalidJson, R, "$", null));
        Assert.StartsWith("invalid JSON: ", MessageOf(Raw("{", NoTechs), DataErrorCode.InvalidJson));
        Expect(Raw("", NoTechs), E(DataErrorCode.InvalidJson, R, "$", null));
        Expect(Raw("﻿" + OneResource, NoTechs), E(DataErrorCode.InvalidJson, R, "$", null));
        Expect(Raw("[]", NoTechs), E(DataErrorCode.UnexpectedType, R, "$", null));
        Assert.Equal("expected object, got array", MessageOf(Raw("[]", NoTechs), DataErrorCode.UnexpectedType));
        Expect(Raw("{}", NoTechs), E(DataErrorCode.MissingProperty, R, "$.resources", null));
        Expect(Raw("""{"resources":{}}""", NoTechs), E(DataErrorCode.UnexpectedType, R, "$.resources", null));
        Expect(Raw("""{"resources":[],"extra":1}""", NoTechs), E(DataErrorCode.UnknownProperty, R, "$.extra", null));
        Expect(Raw("""{"resources":[1]}""", NoTechs), E(DataErrorCode.UnexpectedType, R, "$.resources[0]", null));
        Expect(Raw(OneResource, """{"techs":[], "techs":[]}"""), E(DataErrorCode.DuplicateProperty, T, "$.techs", null));
        Expect(Raw(OneResource + new string(' ', 1048576 - OneResource.Length), NoTechs));
        var big = Raw(OneResource + new string(' ', 1048577 - OneResource.Length), NoTechs);
        Expect(big, E(DataErrorCode.FileTooLarge, R, "$", null));
        Assert.Equal("file has 1048577 bytes (max 1048576)", MessageOf(big, DataErrorCode.FileTooLarge));
        Expect(Raw("""{"resources":[]}""", NoTechs));
        var empty = DataLoader.Parse(Raw("""{"resources":[]}""", NoTechs));
        Assert.Equal(0, empty.Resources.Count);
        Assert.Equal(0, empty.Techs.Count);
        Expect(
            Raw("""{"resources":[{"id":"r_a","category":"basic","storage":"stock","x":[[[[[[[[[[[[[[[[1]]]]]]]]]]]]]]]]}]}""", NoTechs),
            E(DataErrorCode.InvalidJson, R, "$", null));
    }

    [Fact]
    public void ResourceEntryErrors_AreReported()
    {
        // UT-BE-125
        const string ok = "\"category\":\"basic\",\"storage\":\"stock\"";
        var dup = Raw(Resources("{\"id\":\"r_a\"," + ok + "}", "{\"id\":\"r_a\"," + ok + "}"), NoTechs);
        Expect(dup, E(DataErrorCode.DuplicateId, R, "$.resources[1].id", "r_a"));
        Assert.Equal("duplicate id 'r_a' (first at index 0)", MessageOf(dup, DataErrorCode.DuplicateId));
        ExpectRes("{\"id\":\"R-a\"," + ok + "}", E(DataErrorCode.InvalidId, R, "$.resources[0].id", null));
        ExpectRes("{\"id\":\"\"," + ok + "}", E(DataErrorCode.InvalidId, R, "$.resources[0].id", null));
        ExpectRes("{\"id\":\"" + new string('a', 65) + "\"," + ok + "}", E(DataErrorCode.InvalidId, R, "$.resources[0].id", null));
        ExpectRes("{\"id\":\"" + new string('a', 64) + "\"," + ok + "}");
        ExpectRes("""{"id":"r_a","storage":"stock"}""", E(DataErrorCode.MissingProperty, R, "$.resources[0].category", "r_a"));
        ExpectRes("""{"category":"basic","storage":"stock"}""", E(DataErrorCode.MissingProperty, R, "$.resources[0].id", null));
        ExpectRes("""{"id":"r_a","category":"gas","storage":"stock"}""", E(DataErrorCode.InvalidEnumValue, R, "$.resources[0].category", "r_a"));
        ExpectRes("""{"id":"r_a","category":"Basic","storage":"stock"}""", E(DataErrorCode.InvalidEnumValue, R, "$.resources[0].category", "r_a"));
        ExpectRes("""{"id":"r_a","category":"basic","storage":"bag"}""", E(DataErrorCode.InvalidEnumValue, R, "$.resources[0].storage", "r_a"));
        ExpectRes("""{"id":"r_a","category":"basic","storage":"stock","colour":"red"}""", E(DataErrorCode.UnknownProperty, R, "$.resources[0].colour", "r_a"));
        var dupProp = Raw(Resources("""{"id":"r_a","category":"basic","category":"refined","storage":"stock"}"""), NoTechs);
        Expect(dupProp, E(DataErrorCode.DuplicateProperty, R, "$.resources[0].category", "r_a"));
        Assert.Throws<DataValidationException>(() => DataLoader.Parse(dupProp));
        var rn = Raw(Resources("""{"id":"r_a","category":"basic","storage":"stock","renewable":"yes"}"""), NoTechs);
        Expect(rn, E(DataErrorCode.UnexpectedType, R, "$.resources[0].renewable", "r_a"));
        Assert.Equal("expected boolean, got string", MessageOf(rn, DataErrorCode.UnexpectedType));
        var nul = Raw(Resources("""{"id":"r_a","category":"basic","storage":"stock","extractedBy":null}"""), NoTechs);
        Expect(nul, E(DataErrorCode.UnexpectedType, R, "$.resources[0].extractedBy", "r_a"));
        Assert.Equal("expected string, got null", MessageOf(nul, DataErrorCode.UnexpectedType));
        ExpectRes("""{"id":"r_a","category":"raw_hidden","storage":"stock"}""", E(DataErrorCode.MissingRevealedBy, R, "$.resources[0]", "r_a"));
        ExpectRes("""{"id":"r_a","category":"basic","storage":"stock","quotaPerSource":2}""", E(DataErrorCode.QuotaMismatch, R, "$.resources[0].quotaPerSource", "r_a"));
        ExpectRes("""{"id":"r_a","category":"strategic","storage":"quota"}""", E(DataErrorCode.QuotaMismatch, R, "$.resources[0]", "r_a"));
        ExpectRes("""{"id":"r_a","category":"strategic","storage":"quota","quotaPerSource":0}""", E(DataErrorCode.OutOfRange, R, "$.resources[0].quotaPerSource", "r_a"));
        ExpectRes("""{"id":"r_a","category":"strategic","storage":"quota","quotaPerSource":2.5}""", E(DataErrorCode.InvalidNumber, R, "$.resources[0].quotaPerSource", "r_a"));
        ExpectRes("""{"id":"r_a","category":"basic","storage":"stock","extractedBy":"Mine"}""", E(DataErrorCode.InvalidId, R, "$.resources[0].extractedBy", "r_a"));
        var noWarn = Raw(Resources("""{"id":"r_a","category":"basic","storage":"stock","extractedBy":"Mine"}"""), NoTechs);
        foreach (var e in V(noWarn))
        {
            Assert.Equal(DataSeverity.Error, e.Severity);
        }

        ExpectRes(
            """{"colour":1,"id":"r_a","storage":"bag"}""",
            E(DataErrorCode.UnknownProperty, R, "$.resources[0].colour", "r_a"),
            E(DataErrorCode.InvalidEnumValue, R, "$.resources[0].storage", "r_a"),
            E(DataErrorCode.MissingProperty, R, "$.resources[0].category", "r_a"));
    }

    [Fact]
    public void TechEntryErrors_AreReported()
    {
        // UT-BE-126
        const string P = "$.techs[0]";
        ExpectTech(T1(tier: "-1"), E(DataErrorCode.OutOfRange, T, P + ".tier", "t_a"));
        Assert.Equal("tier must be >= 0, got -1", MessageOf(Raw(OneResource, Techs(T1(tier: "-1"))), DataErrorCode.OutOfRange));
        ExpectTech(T1(tier: "1.5"), E(DataErrorCode.InvalidNumber, T, P + ".tier", "t_a"));
        Assert.Equal("expected a 32-bit integer, got 1.5", MessageOf(Raw(OneResource, Techs(T1(tier: "1.5"))), DataErrorCode.InvalidNumber));
        ExpectTech(T1(tier: "\"1\""), E(DataErrorCode.UnexpectedType, T, P + ".tier", "t_a"));
        Assert.Equal("expected integer, got string", MessageOf(Raw(OneResource, Techs(T1(tier: "\"1\""))), DataErrorCode.UnexpectedType));
        ExpectTech(T1(tier: "2147483648"), E(DataErrorCode.InvalidNumber, T, P + ".tier", "t_a"));
        ExpectTech(T1(ticks: "0"), E(DataErrorCode.OutOfRange, T, P + ".researchTicks", "t_a"));
        ExpectTech(T1(cost: """{"r_a":0}"""), E(DataErrorCode.OutOfRange, T, P + ".cost.r_a", "t_a"));
        ExpectTech(T1(cost: """{"R":1}"""), E(DataErrorCode.InvalidId, T, P + ".cost.R", "t_a"));
        ExpectTech(T1(cost: """{"r_a":1,"r_a":2}"""), E(DataErrorCode.DuplicateProperty, T, P + ".cost.r_a", "t_a"));
        ExpectTech(T1(cost: "[]"), E(DataErrorCode.UnexpectedType, T, P + ".cost", "t_a"));
        ExpectTech(T1(requires: "\"t_b\""), E(DataErrorCode.UnexpectedType, T, P + ".requires", "t_a"));
        ExpectTech(T1(requires: "[5]"), E(DataErrorCode.UnexpectedType, T, P + ".requires[0]", "t_a"));
        Expect(
            Raw(OneResource, Techs(T1(), T1(id: "t_b", requires: "[\"t_a\",\"t_a\"]"))),
            E(DataErrorCode.DuplicateReference, T, "$.techs[1].requires[1]", "t_b"));
        ExpectTech(T1(omitEffects: true), E(DataErrorCode.MissingProperty, T, P + ".effects", "t_a"));
        ExpectTech(T1(extra: "\"name\":\"x\""), E(DataErrorCode.UnknownProperty, T, P + ".name", "t_a"));
        Expect(Raw(OneResource, Techs(T1(), T1())), E(DataErrorCode.DuplicateId, T, "$.techs[1].id", "t_a"));
        Expect(
            Raw(Resources("""{"id":"same","category":"basic","storage":"stock"}"""), Techs(T1(id: "same"))));
    }

    [Fact]
    public void EffectErrors_AreReported()
    {
        // UT-BE-127
        const string P = "$.techs[0].effects[0]";
        ExpectEffect("""{"resource":"r_a"}""", E(DataErrorCode.MissingProperty, T, P + ".type", "t_a"));
        ExpectEffect("""{"type":"teleport","to":"x"}""", E(DataErrorCode.UnknownEffectType, T, P + ".type", "t_a"));
        ExpectEffect("""{"type":"revealresource","resource":"r_a"}""", E(DataErrorCode.UnknownEffectType, T, P + ".type", "t_a"));
        ExpectEffect("""{"type":"unlockBuilding"}""", E(DataErrorCode.MissingProperty, T, P + ".building", "t_a"));
        ExpectEffect("""{"type":"unlockBuilding","building":"b_x","unit":"u_y"}""", E(DataErrorCode.UnknownProperty, T, P + ".unit", "t_a"));
        ExpectEffect("""{"type":"raiseGatherCap","resource":"r_a","percent":0}""", E(DataErrorCode.OutOfRange, T, P + ".percent", "t_a"));
        ExpectEffect("""{"type":"raiseGatherCap","resource":"r_a"}""", E(DataErrorCode.MissingProperty, T, P + ".percent", "t_a"));
        ExpectEffect("""{"type":"modifyStat","stat":"armor","add":1}""", E(DataErrorCode.ExactlyOneRequired, T, P, "t_a"));
        Assert.Equal(
            "exactly one of 'unit', 'building' is required",
            MessageOf(Raw(OneResource, Techs(T1(effects: """[{"type":"modifyStat","stat":"armor","add":1}]"""))), DataErrorCode.ExactlyOneRequired));
        ExpectEffect("""{"type":"modifyStat","unit":"u_x","stat":"armor","add":1,"percent":5}""", E(DataErrorCode.ExactlyOneRequired, T, P, "t_a"));
        Assert.Equal(
            "exactly one of 'add', 'percent' is required",
            MessageOf(Raw(OneResource, Techs(T1(effects: """[{"type":"modifyStat","unit":"u_x","stat":"armor","add":1,"percent":5}]"""))), DataErrorCode.ExactlyOneRequired));
        var zero = Raw(OneResource, Techs(T1(effects: """[{"type":"modifyStat","unit":"u_x","stat":"armor","add":0}]""")));
        Expect(zero, E(DataErrorCode.OutOfRange, T, P + ".add", "t_a"));
        Assert.Equal("add must not be 0", MessageOf(zero, DataErrorCode.OutOfRange));
        ExpectEffect("""{"type":"modifyStat","unit":"u_x","add":1}""", E(DataErrorCode.MissingProperty, T, P + ".stat", "t_a"));
        ExpectEffect("""{"type":"obsolete","unit":"u_x","recipe":"x_y"}""", E(DataErrorCode.ExactlyOneRequired, T, P, "t_a"));
        ExpectEffect("""{"type":"replaceRequirement","from":"r_a","to":"r_a"}""", E(DataErrorCode.DuplicateReference, T, P + ".to", "t_a"));
        ExpectTech(T1(effects: "[1]"), E(DataErrorCode.UnexpectedType, T, P, "t_a"));
    }
}
