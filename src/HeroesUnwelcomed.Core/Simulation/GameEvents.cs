using HeroesUnwelcomed.Core.Dungeons;

namespace HeroesUnwelcomed.Core.Simulation;

/// <summary>
/// Something that happened in the run, stamped with the game beat it happened on.
/// </summary>
/// <remarks>
/// Events are how the simulation talks to everything else. Visuals play
/// animations from them, audio reacts to them, and metrics count them. The
/// simulation never knows who's listening. Every event is also kept in
/// <see cref="Run.Events"/>, which is the run's complete history.
/// </remarks>
public abstract record GameEvent(double Beat);

// ---- Parties -------------------------------------------------------------

/// <summary>A new party entered at the entrance. <paramref name="HeroIds"/> in marching order.</summary>
public sealed record PartySpawned(double Beat, int PartyId, IReadOnlyList<int> HeroIds) : GameEvent(Beat);

/// <summary>A party left one room for the next. It arrives at <paramref name="ArriveBeat"/>.</summary>
public sealed record PartyDeparted(double Beat, int PartyId, int FromRoomId, int ToRoomId, ConnectionKind Connection, double ArriveBeat) : GameEvent(Beat);

/// <summary>The next room is occupied by a party ahead, so this one waits where it is.</summary>
public sealed record PartyWaiting(double Beat, int PartyId, int RoomId, int BlockedByPartyId) : GameEvent(Beat);

public sealed record PartyArrived(double Beat, int PartyId, int RoomId) : GameEvent(Beat);

/// <summary>The party gathers to decide who goes first.</summary>
public sealed record PartyHuddleStarted(double Beat, int PartyId, int RoomId) : GameEvent(Beat);

/// <summary>The huddle's decision: <paramref name="HeroIds"/> in attempt order.</summary>
public sealed record PartyReordered(double Beat, int PartyId, int RoomId, IReadOnlyList<int> HeroIds) : GameEvent(Beat);

/// <summary>Every member died; the party is over.</summary>
public sealed record PartyWiped(double Beat, int PartyId, int RoomId) : GameEvent(Beat);

// ---- Attempts ------------------------------------------------------------

public sealed record AttemptStarted(double Beat, int PartyId, int HeroId, int RoomId) : GameEvent(Beat);

/// <summary>A hero beat the room; the whole party may pass.</summary>
public sealed record RoomCleared(double Beat, int PartyId, int HeroId, int RoomId, string RoomKind, double Chance) : GameEvent(Beat);

/// <summary>A hero failed and died.</summary>
public sealed record HeroDied(double Beat, int PartyId, int HeroId, int RoomId, string RoomKind, double Chance, double RespawnAtBeat) : GameEvent(Beat);

public sealed record HeroRespawned(double Beat, int HeroId) : GameEvent(Beat);

// ---- Player and economy ---------------------------------------------------

/// <summary>The soul balance changed. Positive = earned, negative = spent.</summary>
public sealed record SoulsChanged(double Beat, int Amount, int Balance, string Reason, int? RoomId) : GameEvent(Beat);

public sealed record DoorStateChanged(double Beat, DoorState State) : GameEvent(Beat);

public sealed record RoomPlaced(double Beat, int RoomId, string RoomKind, int OrderIndex) : GameEvent(Beat);

public sealed record RoomUpgraded(double Beat, int RoomId, int Level) : GameEvent(Beat);

public sealed record RoomFlipped(double Beat, int RoomId) : GameEvent(Beat);

/// <summary>The run is over: a hero defeated the treasure room.</summary>
public sealed record RunEnded(double Beat, int WinningHeroId, int WinningPartyId) : GameEvent(Beat);
