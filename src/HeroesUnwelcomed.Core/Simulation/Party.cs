using HeroesUnwelcomed.Core.Heroes;

namespace HeroesUnwelcomed.Core.Simulation;

/// <summary>What a party is doing right now.</summary>
public enum PartyPhase
{
    /// <summary>Moving to the room at <see cref="Party.RoomIndex"/> (hallway or portal).</summary>
    Traveling,

    /// <summary>Done here, but the next room is occupied. Idles until it frees up.</summary>
    Waiting,

    /// <summary>Gathered at the room, deciding the order.</summary>
    Huddling,

    /// <summary>One hero is attempting the room.</summary>
    Attempting,

    /// <summary>Wiped out, or reached the end.</summary>
    Finished,
}

/// <summary>A group of heroes moving through the dungeon together.</summary>
public sealed class Party
{
    internal Party(int id, List<Hero> members)
    {
        Id = id;
        Members = members;
    }

    public int Id { get; }

    /// <summary>Living members, in the current marching/attempt order.</summary>
    public List<Hero> Members { get; internal set; }

    /// <summary>
    /// Index (in dungeon order) of the room the party is at, or is traveling to.
    /// A party "holds" this room, so no other party can enter it.
    /// </summary>
    public int RoomIndex { get; internal set; }

    public PartyPhase Phase { get; internal set; }

    /// <summary>Game beat when the current phase ends (unused while Waiting/Finished).</summary>
    public double PhaseEndsAt { get; internal set; }

    public Hero? Attempter => Phase == PartyPhase.Attempting ? Members.FirstOrDefault() : null;

    public bool IsActive => Phase != PartyPhase.Finished;

    // Set while waiting, so the "waiting" event is only reported once.
    internal bool ReportedWaiting { get; set; }

    public override string ToString() => $"Party#{Id} [{string.Join(", ", Members)}] {Phase} @room {RoomIndex}";
}
