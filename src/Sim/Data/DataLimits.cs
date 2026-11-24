namespace Rts.Sim.Data;

public static class DataLimits
{
    public const int MaxFileBytes = 1_048_576;   // proposta §7: 1 MiB por arquivo
    public const int MaxIdLength = 64;
    public const int MaxJsonDepth = 16;
}
