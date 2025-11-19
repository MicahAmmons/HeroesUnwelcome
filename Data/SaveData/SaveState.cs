using System.Collections.Generic;

public class SaveState
{
    public EncounterSaveData Encounters { get; set; }
}

public class EncounterSaveData
{
    public Dictionary<string, bool> Combat { get; set; } = new();
    public Dictionary<string, bool> Puzzle { get; set; } = new();
    public Dictionary<string, bool> Trap { get; set; } = new();
    public Dictionary<string, bool> LockedDoor { get; set; } = new();
}
