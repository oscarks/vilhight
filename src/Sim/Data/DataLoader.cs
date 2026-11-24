using System;
using System.IO;
using System.Text;

namespace Rts.Sim.Data;

// design: ADR-005 (orientado a dados, validação no carregamento).
public static class DataLoader
{
    public static DataSet Load(string dataDir)
    {
        ArgumentNullException.ThrowIfNull(dataDir);
        var resources = ReadFile(Path.Combine(dataDir, DataFiles.Resources), out var resBytes);
        var techs = ReadFile(Path.Combine(dataDir, DataFiles.Techs), out var techBytes);
        return Finish(DataCompiler.Compile(new RawData(resources, techs), resBytes, techBytes));
    }

    public static DataSet Parse(RawData raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        return Finish(DataCompiler.Compile(raw, -1, -1));
    }

    private static string? ReadFile(string path, out long oversizeBytes)
    {
        oversizeBytes = -1;
        if (!File.Exists(path))
        {
            return null;
        }

        var length = new FileInfo(path).Length;
        if (length > DataLimits.MaxFileBytes)
        {
            oversizeBytes = length;
            return null;
        }

        return File.ReadAllText(path, Encoding.UTF8);
    }

    private static DataSet Finish(DataCompiler.Result result)
    {
        var errors = new System.Collections.Generic.List<DataError>();
        var warnings = new System.Collections.Generic.List<DataError>();
        foreach (var item in result.Items)
        {
            (item.Severity == DataSeverity.Error ? errors : warnings).Add(item);
        }

        if (errors.Count > 0)
        {
            throw new DataValidationException(errors.ToArray(), warnings.ToArray());
        }

        return result.Set!;
    }
}
