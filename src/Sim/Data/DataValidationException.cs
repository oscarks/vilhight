using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Rts.Sim.Data;

public sealed class DataValidationException : Exception
{
    internal DataValidationException(DataError[] errors, DataError[] warnings)
        : base(BuildMessage(errors))
    {
        Errors = Array.AsReadOnly(errors);
        Warnings = Array.AsReadOnly(warnings);
    }

    public IReadOnlyList<DataError> Errors { get; }
    public IReadOnlyList<DataError> Warnings { get; }

    private static string BuildMessage(DataError[] errors)
    {
        var sb = new StringBuilder();
        sb.Append(string.Create(CultureInfo.InvariantCulture, $"data validation failed with {errors.Length} error(s):"));
        foreach (var e in errors)
        {
            sb.Append('\n').Append(e.ToString());
        }

        return sb.ToString();
    }
}
