using System;
using System.Collections.Generic;

namespace Rts.Sim.Data;

public static class DataValidator
{
    // Erros e avisos, na ordem de emissão (RN-P3-02).
    public static IReadOnlyList<DataError> Validate(RawData raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        return Array.AsReadOnly(DataCompiler.Compile(raw, -1, -1).Items);
    }
}
