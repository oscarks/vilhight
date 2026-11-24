using System.Globalization;

namespace Rts.Sim.Data;

public enum DataSeverity : byte { Error = 1, Warning = 2 }

public enum DataErrorCode
{
    FileMissing = 1,
    FileTooLarge = 2,
    InvalidJson = 3,
    UnexpectedType = 4,
    MissingProperty = 5,
    UnknownProperty = 6,
    DuplicateProperty = 7,
    InvalidId = 8,
    DuplicateId = 9,
    InvalidEnumValue = 10,
    InvalidNumber = 11,
    OutOfRange = 12,
    UnknownEffectType = 13,
    ExactlyOneRequired = 14,
    QuotaMismatch = 15,
    MissingRevealedBy = 16,
    UnknownResource = 17,
    UnknownTech = 18,
    DuplicateReference = 19,
    MissingRevealEffect = 20,
    RevealEffectMismatch = 21,
    TechCycle = 22,
    UnreachableResource = 23,
    UnresearchableTech = 24,
    PendingReference = 25,   // único código de Severity Warning
}

// Id: id da entrada dona do Path, se tiver formato válido (mesmo se duplicado); senão null.
public sealed record DataError(DataSeverity Severity, DataErrorCode Code, string File, string Path,
    string? Id, string Message)
{
    public override string ToString()
    {
        var sev = Severity == DataSeverity.Error ? "error" : "warning";
        return string.Create(CultureInfo.InvariantCulture, $"{sev} {File} {Path} id={Id ?? "-"} {Code}: {Message}");
    }
}
