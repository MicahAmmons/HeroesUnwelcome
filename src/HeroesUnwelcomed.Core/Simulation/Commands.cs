using HeroesUnwelcomed.Core.Definitions;

namespace HeroesUnwelcomed.Core.Simulation;

/// <summary>
/// Something the player (or the debug panel, or a replay, or the SimRunner's
/// bot) asks the run to do. Commands are requests: the run checks them and
/// either applies them or refuses with a reason.
/// </summary>
public abstract record GameCommand;

/// <summary>Build a new room. Only while the door is locked.</summary>
public sealed record PlaceRoom(string Kind, GridPoint Position, Facing Facing = Facing.Right) : GameCommand;

/// <summary>Raise a room's level (higher difficulty). Only while the door is locked.</summary>
public sealed record UpgradeRoom(int RoomId) : GameCommand;

/// <summary>Turn a room to face the other way. Only while the door is locked.</summary>
public sealed record FlipRoom(int RoomId) : GameCommand;

/// <summary>Stop new parties entering; once the current ones finish, the door is locked and building is allowed.</summary>
public sealed record LockDoor : GameCommand;

/// <summary>Open the dungeon to heroes again.</summary>
public sealed record UnlockDoor : GameCommand;

/// <summary>Send in the next party from the hero pool now.</summary>
public sealed record SendNextParty : GameCommand;

/// <summary>Whether a command was applied, and if not, why.</summary>
public readonly record struct CommandResult(bool Accepted, string? Reason)
{
    public static CommandResult Ok => new(true, null);

    public static CommandResult Refused(string reason) => new(false, reason);

    public override string ToString() => Accepted ? "OK" : $"Refused: {Reason}";
}

/// <summary>A command and the beat it was applied on. The list of these is a replay.</summary>
public readonly record struct RecordedCommand(double Beat, GameCommand Command);

/// <summary>
/// The dungeon door. Building happens only while it's locked, so no party is
/// ever inside a room that changes.
/// </summary>
public enum DoorState
{
    /// <summary>Heroes may enter. No building.</summary>
    Open,

    /// <summary>No new parties; waiting for the ones inside to finish.</summary>
    Locking,

    /// <summary>Empty and shut: build, upgrade and flip freely.</summary>
    Locked,
}
