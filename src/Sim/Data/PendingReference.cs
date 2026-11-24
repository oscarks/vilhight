namespace Rts.Sim.Data;

public enum ContentKind : byte
{
    Building = 1,   // validado quando buildings.json existir (E5)
    Unit = 2,       // units.json (E5)
    Recipe = 3,     // recipes.json (E5)
}

// File: DataFiles.*; Path: caminho JSON do valor; OwnerKey: id da entrada que contém a referência.
public sealed record PendingReference(ContentKind Kind, string TargetKey, string File, string Path, string OwnerKey);
