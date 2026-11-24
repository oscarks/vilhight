using System;
using System.IO;
using Rts.Sim.Core;
using Rts.Sim.Data;
using Rts.Sim.State;
using Rts.Sim.Systems;
using Rts.Sim.Tests.Architecture;
using Rts.Sim.Tests.Fixtures;
using Xunit;

namespace Rts.Sim.Tests.Data;

public sealed class GameDataTests
{
    private static string DataDir => Path.Combine(ForbiddenApiScanner.FindRepoRoot(), "game", "data");

    [Fact]
    public void GameData_LoadsSampleSet()
    {
        // E2E-BE-10
        var data = DataLoader.Load(DataDir);
        Assert.Equal(5, data.Resources.Count);
        Assert.Equal(
            new[] { "food", "wood", "knowledge", "ore", "metal" },
            new[] { data.Resources.All[0].Key, data.Resources.All[1].Key, data.Resources.All[2].Key, data.Resources.All[3].Key, data.Resources.All[4].Key });
        Assert.Equal(4, data.Techs.Count);
        Assert.Equal(
            new[] { "tech_mining", "tech_smelting", "tech_agriculture", "tech_forestry" },
            new[] { data.Techs.All[0].Key, data.Techs.All[1].Key, data.Techs.All[2].Key, data.Techs.All[3].Key });
        var ore = data.Resources.All[3];
        Assert.Equal(ResourceCategory.RawHidden, ore.Category);
        Assert.Equal(ResourceStorage.Stock, ore.Storage);
        Assert.True(ore.Depletes);
        Assert.Equal(new TechId(0), ore.RevealedBy);
        Assert.Equal("mine", ore.ExtractedBy);
        var smelting = data.Techs.All[1];
        Assert.Equal(new[] { new TechId(0) }, smelting.Requires);
        Assert.Equal(
            new[]
            {
                new ResourceAmount(new ResourceId(2), Fixed.FromInt(120)),
                new ResourceAmount(new ResourceId(3), Fixed.FromInt(50)),
            },
            smelting.Cost);
        Assert.Equal(
            new TechEffect[]
            {
                new UnlockBuildingEffect("smelter"),
                new UnlockRecipeEffect("smelt_metal"),
                new RaiseGatherCapEffect(new ResourceId(1), 25),
            },
            smelting.Effects);
        Assert.Equal(4, data.Warnings.Count);
        Assert.Equal(
            new[]
            {
                new PendingReference(ContentKind.Building, "mine", DataFiles.Resources, "$.resources[3].extractedBy", "ore"),
                new PendingReference(ContentKind.Building, "mine", DataFiles.Techs, "$.techs[0].effects[1].building", "tech_mining"),
                new PendingReference(ContentKind.Building, "smelter", DataFiles.Techs, "$.techs[1].effects[0].building", "tech_smelting"),
                new PendingReference(ContentKind.Recipe, "smelt_metal", DataFiles.Techs, "$.techs[1].effects[1].recipe", "tech_smelting"),
            },
            data.PendingReferences);
        var raw = new RawData(
            File.ReadAllText(Path.Combine(DataDir, DataFiles.Resources)),
            File.ReadAllText(Path.Combine(DataDir, DataFiles.Techs)));
        foreach (var e in DataValidator.Validate(raw))
        {
            Assert.Equal(DataSeverity.Warning, e.Severity);
        }
    }

    [Fact]
    public void GameData_StateRunsWithLoadedData()
    {
        // E2E-BE-11
        var data = DataLoader.Load(DataDir);
        var sim = new Simulation(
            GameState.Create(SetupFactory.OneSystemThreeCountries(42), data),
            Array.Empty<ISimSystem>(),
            TestCommands.CreateRegistry());
        Assert.Equal(Refs.S0Hash, sim.ComputeHash());
        Assert.True(data.Resources.TryFind("ore", out var ore));
        Assert.Equal(new ResourceId(3), ore);
        var events = sim.Step(new Rts.Sim.Commands.Command[] { Refs.AddStock(1, Refs.Human1, ore.Value, Fixed.FromInt(7)) });
        Assert.Equal(1, events.AppliedCount);
        Assert.Equal(Fixed.FromInt(7), sim.State.FindCountry(new CountryId(1))!.GetStock(ore));
    }
}
