namespace Rts.Sim.Data;

// Conteúdo textual dos arquivos. null = arquivo ausente (FileMissing).
public sealed record RawData(string? ResourcesJson, string? TechsJson);
