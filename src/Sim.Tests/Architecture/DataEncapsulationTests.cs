using System;
using System.Collections.Generic;
using System.Reflection;
using Rts.Sim.Data;
using Rts.Sim.State;
using Rts.Sim.Tests.Fixtures;
using Xunit;

namespace Rts.Sim.Tests.Architecture;

public sealed class DataEncapsulationTests
{
    [Fact]
    public void DataTypes_AreReadOnly()
    {
        // UT-BE-140
        var types = new[]
        {
            typeof(DataSet), typeof(ResourceRegistry), typeof(TechRegistry), typeof(ResourceDef),
            typeof(TechDef), typeof(DataValidationException),
        };
        foreach (var t in types)
        {
            Assert.Empty(t.GetConstructors());
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Assert.True(p.SetMethod == null || !p.SetMethod.IsPublic, t.Name + "." + p.Name);
            }
        }

        var data = DataLoader.Parse(DataFixtures.Base());
        NotMutable<ResourceDef>(data.Resources.All, data.Resources.All[0]);
        NotMutable<TechDef>(data.Techs.All, data.Techs.All[0]);
        NotMutable<ResourceAmount>(data.Techs.All[0].Cost, default);
        NotMutable<TechId>(data.Techs.All[1].Requires, default);
        NotMutable<TechEffect>(data.Techs.All[0].Effects, data.Techs.All[0].Effects[0]);
        NotMutable<DataError>(data.Warnings, data.Warnings[0]);
        NotMutable<PendingReference>(data.PendingReferences, data.PendingReferences[0]);

        Assert.True(typeof(TechEffect).IsAbstract);
        var ctors = typeof(TechEffect).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        // O construtor de cópia protegido é sintetizado pelo compilador para todo record não selado.
        Assert.All(ctors, c => Assert.False(c.IsPublic));
        Assert.Contains(ctors, c => c.GetParameters().Length == 0 && c.IsFamilyAndAssembly);
    }

    private static void NotMutable<T>(IReadOnlyList<T> list, T item)
    {
        Assert.False(list is T[]);
        Assert.False(list is List<T>);
        Assert.Throws<NotSupportedException>(() => ((IList<T>)list).Add(item));
    }
}
