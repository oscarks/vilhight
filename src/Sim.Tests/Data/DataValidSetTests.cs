using System;
using System.Collections.Generic;
using Rts.Sim.Core;
using Rts.Sim.Data;
using Rts.Sim.State;
using Rts.Sim.Tests.Fixtures;
using Xunit;
using static Rts.Sim.Tests.Data.D;

namespace Rts.Sim.Tests.Data;

public sealed class DataValidSetTests
{
    [Fact]
    public void Parse_BaseFixture_BuildsRegistries()
    {
        // UT-BE-120
        var data = DataLoader.Parse(DataFixtures.Base());
        Assert.Equal(5, data.Resources.Count);
        Assert.Equal(3, data.Techs.Count);
        var keys = new List<string>();
        for (var i = 0; i < data.Resources.All.Count; i++)
        {
            keys.Add(data.Resources.All[i].Key);
            Assert.Equal(i, data.Resources.All[i].Id.Value);
        }

        Assert.Equal(new[] { "r_grain", "r_lore", "r_rock", "r_alloy", "r_gem" }, keys);

        var grain = data.Resources.All[0];
        Assert.Equal(ResourceCategory.Basic, grain.Category);
        Assert.Equal(ResourceStorage.Stock, grain.Storage);
        Assert.True(grain.Renewable);
        Assert.False(grain.Depletes);
        Assert.Null(grain.RevealedBy);
        Assert.Null(grain.ExtractedBy);
        Assert.Equal(0, grain.QuotaPerSource);

        var rock = data.Resources.All[2];
        Assert.Equal(ResourceCategory.RawHidden, rock.Category);
        Assert.Equal(ResourceStorage.Stock, rock.Storage);
        Assert.True(rock.Depletes);
        Assert.Equal(new TechId(0), rock.RevealedBy);
        Assert.Equal("b_pit", rock.ExtractedBy);

        var gem = data.Resources.All[4];
        Assert.Equal(ResourceCategory.Strategic, gem.Category);
        Assert.Equal(ResourceStorage.Quota, gem.Storage);
        Assert.Equal(new TechId(2), gem.RevealedBy);
        Assert.Equal(4, gem.QuotaPerSource);

        var dig = data.Techs[new TechId(0)];
        Assert.Equal(1, dig.Tier);
        Assert.Equal(600, dig.ResearchTicks);
        Assert.Empty(dig.Requires);
        Assert.Equal(new[] { new ResourceAmount(new ResourceId(1), Fixed.FromInt(50)) }, dig.Cost);
        Assert.Equal(new TechEffect[] { new RevealResourceEffect(new ResourceId(2)), new UnlockBuildingEffect("b_pit") }, dig.Effects);

        var melt = data.Techs[new TechId(1)];
        Assert.Equal(2, melt.Tier);
        Assert.Equal(900, melt.ResearchTicks);
        Assert.Equal(new[] { new TechId(0) }, melt.Requires);
        Assert.Equal(
            new[]
            {
                new ResourceAmount(new ResourceId(1), Fixed.FromInt(120)),
                new ResourceAmount(new ResourceId(2), Fixed.FromInt(50)),
            },
            melt.Cost);

        Assert.True(data.Resources.TryFind("r_alloy", out var id));
        Assert.Equal(new ResourceId(3), id);
        Assert.False(data.Resources.TryFind("nope", out _));
        Assert.True(data.Techs.TryFind("t_gem", out var t));
        Assert.Equal(new TechId(2), t);

        Assert.Throws<ArgumentOutOfRangeException>(() => data.Resources[new ResourceId(5)]);
        Assert.Throws<ArgumentOutOfRangeException>(() => data.Techs[new TechId(-1)]);
        Assert.Throws<ArgumentNullException>(() => data.Resources.TryFind(null!, out _));
        Assert.Throws<ArgumentNullException>(() => data.Techs.TryFind(null!, out _));

        foreach (var e in V(DataFixtures.Base()))
        {
            Assert.NotEqual(DataSeverity.Error, e.Severity);
        }
    }

    [Fact]
    public void Parse_IsCanonical()
    {
        // UT-BE-121
        const string resources = """
            {"resources":[
              {"storage":"stock","category":"basic","renewable":true,"id":"r_grain"},
              {"storage":"stock","category":"abstract","id":"r_lore"},
              {"depletes":true,"extractedBy":"b_pit","revealedBy":"t_dig","storage":"stock","category":"raw_hidden","id":"r_rock"},
              {"storage":"stock","category":"refined","id":"r_alloy"},
              {"quotaPerSource":4,"revealedBy":"t_gem","storage":"quota","category":"strategic","id":"r_gem"}
            ]}
            """;
        const string techs = """
            {"techs":[
              {"effects":[{"resource":"r_rock","type":"revealResource"},{"building":"b_pit","type":"unlockBuilding"}],
               "requires":[],"researchTicks":600,"cost":{"r_lore":50},"tier":1,"id":"t_dig"},

              {"effects":[{"recipe":"x_melt","type":"unlockRecipe"},{"percent":25,"resource":"r_grain","type":"raiseGatherCap"},
                          {"unit":"u_guard","type":"unlockUnit"}],
               "requires":["t_dig"],"researchTicks":900,"cost":{"r_lore":120,"r_rock":50},"tier":2,"id":"t_melt"},
              {"effects":[{"type":"revealResource","resource":"r_gem"},
                          {"stat":"armor","add":1,"unit":"u_guard","type":"modifyStat"},
                          {"to":"r_alloy","from":"r_grain","type":"replaceRequirement"},
                          {"unit":"u_guard","type":"obsolete"}],
               "requires":["t_melt"],"researchTicks":1200,"cost":{"r_alloy":10,"r_lore":200},"tier":3,"id":"t_gem"}
            ]}
            """;
        var baseline = DataFixtures.Dump(DataLoader.Parse(DataFixtures.Base()));
        Assert.Equal(baseline, DataFixtures.Dump(DataLoader.Parse(DataFixtures.Raw(resources, techs))));

        var swapped = DataFixtures.Techs(
            DataFixtures.T1(id: "t_melt", requires: "[]"),
            DataFixtures.T1(id: "t_dig"));
        var data = DataLoader.Parse(DataFixtures.Raw(DataFixtures.OneResource, swapped));
        Assert.Equal(new TechId(0), Assert.IsType<TechId>(data.Techs.All[0].Id));
        Assert.Equal("t_melt", data.Techs.All[0].Key);
        Assert.NotEqual(baseline, DataFixtures.Dump(data));
    }

    [Fact]
    public void Parse_RecordsPendingReferencesAsWarnings()
    {
        // UT-BE-122
        var data = DataLoader.Parse(DataFixtures.Base());
        var expected = new[]
        {
            new PendingReference(ContentKind.Building, "b_pit", R, "$.resources[2].extractedBy", "r_rock"),
            new PendingReference(ContentKind.Building, "b_pit", T, "$.techs[0].effects[1].building", "t_dig"),
            new PendingReference(ContentKind.Recipe, "x_melt", T, "$.techs[1].effects[0].recipe", "t_melt"),
            new PendingReference(ContentKind.Unit, "u_guard", T, "$.techs[1].effects[2].unit", "t_melt"),
            new PendingReference(ContentKind.Unit, "u_guard", T, "$.techs[2].effects[1].unit", "t_gem"),
            new PendingReference(ContentKind.Unit, "u_guard", T, "$.techs[2].effects[3].unit", "t_gem"),
        };
        Assert.Equal(expected, data.PendingReferences);
        Assert.Equal(6, data.Warnings.Count);
        foreach (var w in data.Warnings)
        {
            Assert.Equal(DataSeverity.Warning, w.Severity);
            Assert.Equal(DataErrorCode.PendingReference, w.Code);
        }

        Assert.Equal(
            "warning resources.json $.resources[2].extractedBy id=r_rock PendingReference: building 'b_pit' is not validated yet",
            data.Warnings[0].ToString());
        Assert.Equal(6, V(DataFixtures.Base()).Count);
    }

    [Fact]
    public void Parse_AllEightEffectTypes()
    {
        // UT-BE-123
        var data = DataLoader.Parse(DataFixtures.Base());
        var gem = data.Techs[new TechId(2)];
        Assert.Equal(
            new TechEffect[]
            {
                new RevealResourceEffect(new ResourceId(4)),
                new ModifyStatEffect(ContentKind.Unit, "u_guard", "armor", StatModifierKind.Add, 1),
                new ReplaceRequirementEffect(new ResourceId(0), new ResourceId(3)),
                new ObsoleteEffect(ContentKind.Unit, "u_guard"),
            },
            gem.Effects);
        var melt = data.Techs[new TechId(1)];
        Assert.Equal(
            new TechEffect[]
            {
                new UnlockRecipeEffect("x_melt"),
                new RaiseGatherCapEffect(new ResourceId(0), 25),
                new UnlockUnitEffect("u_guard"),
            },
            melt.Effects);

        var types = new List<TechEffectType>();
        foreach (var e in gem.Effects)
        {
            types.Add(e.Type);
        }

        foreach (var e in melt.Effects)
        {
            types.Add(e.Type);
        }

        Assert.Equal(
            new[]
            {
                TechEffectType.RevealResource, TechEffectType.ModifyStat, TechEffectType.ReplaceRequirement,
                TechEffectType.Obsolete, TechEffectType.UnlockRecipe, TechEffectType.RaiseGatherCap,
                TechEffectType.UnlockUnit,
            },
            types);
        Assert.Equal(8, Enum.GetValues<TechEffectType>().Length);
        Assert.Equal(1, (int)TechEffectType.RevealResource);
        Assert.Equal(8, (int)TechEffectType.Obsolete);

        var raw = DataFixtures.Raw(
            DataFixtures.OneResource,
            DataFixtures.Techs(DataFixtures.T1(effects:
                """[{"type":"modifyStat","building":"b_x","stat":"hp","percent":-10},{"type":"obsolete","recipe":"x_y"}]""")));
        var d2 = DataLoader.Parse(raw);
        Assert.Equal(
            new TechEffect[]
            {
                new ModifyStatEffect(ContentKind.Building, "b_x", "hp", StatModifierKind.Percent, -10),
                new ObsoleteEffect(ContentKind.Recipe, "x_y"),
            },
            d2.Techs.All[0].Effects);
    }
}
