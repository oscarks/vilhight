using System;
using System.Collections.Generic;

namespace Rts.Sim.Data;

public sealed class DataSet
{
    internal DataSet(ResourceRegistry resources, TechRegistry techs,
        DataError[] warnings, PendingReference[] pendingReferences)
    {
        Resources = resources;
        Techs = techs;
        Warnings = Array.AsReadOnly(warnings);
        PendingReferences = Array.AsReadOnly(pendingReferences);
    }

    public ResourceRegistry Resources { get; }
    public TechRegistry Techs { get; }
    public IReadOnlyList<DataError> Warnings { get; }
    public IReadOnlyList<PendingReference> PendingReferences { get; }
}
