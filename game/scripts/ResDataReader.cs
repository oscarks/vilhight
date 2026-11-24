using Godot;
using Rts.Sim.Data;

namespace Rts.Game;

// ADR-009: lê os dados de res:// (editor: pasta game/data; exportado: Rts.pck) e entrega o texto à Sim.
public static class ResDataReader
{
    // resDir sem barra final, ex.: "res://data". Arquivo ausente => null (a Sim reporta FileMissing).
    public static RawData Read(string resDir) =>
        new(ReadText(resDir + "/" + DataFiles.Resources), ReadText(resDir + "/" + DataFiles.Techs));

    // Lê no máximo DataLimits.MaxFileBytes + 1 bytes; remove BOM UTF-8; decodifica sem lançar em bytes inválidos.
    internal static string? ReadText(string resPath)
    {
        if (!FileAccess.FileExists(resPath))
        {
            return null;
        }

        using var file = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            GD.PushError(FileAccess.GetOpenError().ToString());
            return null;
        }

        var bytes = file.GetBuffer(DataLimits.MaxFileBytes + 1);
        var offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
        return new System.Text.UTF8Encoding(false, false).GetString(bytes, offset, bytes.Length - offset);
    }
}
