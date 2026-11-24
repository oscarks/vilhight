using System;
using System.IO;
using System.Text;
using Rts.Sim.Data;
using Xunit;
using static Rts.Sim.Tests.Data.D;
using static Rts.Sim.Tests.Fixtures.DataFixtures;

namespace Rts.Sim.Tests.Data;

public sealed class DataLoaderTests
{
    [Fact]
    public void Load_ReadsDirectory()
    {
        // UT-BE-135
        var expected = Dump(DataLoader.Parse(Base()));
        using (var dir = WriteTemp(BaseResources, BaseTechs))
        {
            Assert.Equal(expected, Dump(DataLoader.Load(dir.Path)));
        }

        using (var dir = WriteTemp(null, null))
        {
            var bom = new UTF8Encoding(true);
            File.WriteAllText(System.IO.Path.Combine(dir.Path, DataFiles.Resources), BaseResources, bom);
            File.WriteAllText(System.IO.Path.Combine(dir.Path, DataFiles.Techs), BaseTechs, bom);
            Assert.Equal(expected, Dump(DataLoader.Load(dir.Path)));
        }
    }

    [Fact]
    public void Load_MissingFiles_AreReported()
    {
        // UT-BE-136
        using var dir = WriteTemp(BaseResources, null);
        var ex = Assert.Throws<DataValidationException>(() => DataLoader.Load(dir.Path));
        Assert.Single(ex.Errors);
        Assert.Equal((DataErrorCode.FileMissing, T, "$", (string?)null), (ex.Errors[0].Code, ex.Errors[0].File, ex.Errors[0].Path, ex.Errors[0].Id));

        var ex2 = Assert.Throws<DataValidationException>(() => DataLoader.Load(System.IO.Path.Combine(dir.Path, "nope")));
        Assert.Equal(2, ex2.Errors.Count);
        Assert.Equal(DataErrorCode.FileMissing, ex2.Errors[0].Code);
        Assert.Equal(R, ex2.Errors[0].File);
        Assert.Equal(DataErrorCode.FileMissing, ex2.Errors[1].Code);
        Assert.Equal(T, ex2.Errors[1].File);
    }

    [Fact]
    public void Load_TooLargeFileIsNotParsed()
    {
        // UT-BE-137
        using var dir = WriteTemp("{" + new string(' ', 1048576), NoTechs);
        var ex = Assert.Throws<DataValidationException>(() => DataLoader.Load(dir.Path));
        Assert.Single(ex.Errors);
        Assert.Equal(DataErrorCode.FileTooLarge, ex.Errors[0].Code);
        Assert.Equal(R, ex.Errors[0].File);
        Assert.Equal("$", ex.Errors[0].Path);
        Assert.Equal("file has 1048577 bytes (max 1048576)", ex.Errors[0].Message);
    }

    [Fact]
    public void Load_NullArgument_Throws()
    {
        // UT-BE-138
        var ex = Assert.Throws<ArgumentNullException>(() => DataLoader.Load(null!));
        Assert.Equal("dataDir", ex.ParamName);
    }
}
