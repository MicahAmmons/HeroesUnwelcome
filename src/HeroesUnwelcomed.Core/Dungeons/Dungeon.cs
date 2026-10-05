using HeroesUnwelcomed.Core.Definitions;

namespace HeroesUnwelcomed.Core.Dungeons;

/// <summary>How heroes get from one room to the next one in the order.</summary>
public enum ConnectionKind
{
    /// <summary>The next room is right next door, in the direction of travel: walk down a hallway, through its door.</summary>
    Hallway,

    /// <summary>Anywhere else (further away, above/below, or behind): step through a portal.</summary>
    Portal,
}

/// <summary>A room placed in the dungeon.</summary>
public sealed class Room
{
    internal Room(int id, RoomKind kind, GridPoint position, Facing facing)
    {
        Id = id;
        Kind = kind;
        Position = position;
        Facing = facing;
    }

    /// <summary>Unique for the whole run; never reused, even if the room order changes.</summary>
    public int Id { get; }
    public RoomKind Kind { get; }
    public GridPoint Position { get; }
    public Facing Facing { get; internal set; }
    public int Level { get; internal set; }

    public double Difficulty => Kind.DifficultyAt(Level);

    public override string ToString() => $"{Kind.Id}#{Id} at {Position}";
}

/// <summary>
/// The player's dungeon: a capped grid holding rooms, and the order heroes
/// visit them in. The entrance is always first and the treasure room always last.
/// </summary>
/// <remarks>
/// Order and position are separate on purpose. Position is where a room sits on
/// the grid; order is the path. Connections between consecutive rooms are
/// <b>calculated</b> from both, never stored, so any change to the layout or
/// order automatically gives the right hallways and portals.
/// </remarks>
public sealed class Dungeon
{
    private readonly List<Room> _ordered = [];
    private int _nextRoomId = 1;

    public Dungeon(int width, int height)
    {
        if (width < 2 || height < 1) throw new ArgumentOutOfRangeException(nameof(width), "The grid must be at least 2x1.");
        Width = width;
        Height = height;
    }

    public int Width { get; }
    public int Height { get; }

    /// <summary>Rooms in the order heroes visit them.</summary>
    public IReadOnlyList<Room> Rooms => _ordered;

    public Room Entrance => _ordered[0];
    public Room Treasure => _ordered[^1];

    public bool IsTreasure(int orderIndex) => orderIndex == _ordered.Count - 1;

    public Room? RoomAt(GridPoint position) => _ordered.FirstOrDefault(r => r.Position == position);

    public Room? RoomById(int id) => _ordered.FirstOrDefault(r => r.Id == id);

    public int IndexOf(Room room) => _ordered.IndexOf(room);

    public bool InBounds(GridPoint p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

    /// <summary>Why a room couldn't go at <paramref name="position"/>, or null if it can.</summary>
    public string? WhyCantPlace(GridPoint position)
    {
        if (!InBounds(position)) return $"{position} is outside the {Width}x{Height} grid.";
        if (RoomAt(position) is { } existing) return $"{position} already has {existing.Kind.Id}.";
        return null;
    }

    /// <summary>Adds a room at a given place in the order (0 = first).</summary>
    public Room Insert(int orderIndex, RoomKind kind, GridPoint position, Facing facing)
    {
        if (WhyCantPlace(position) is { } reason) throw new InvalidOperationException(reason);
        var room = new Room(_nextRoomId++, kind, position, facing);
        _ordered.Insert(orderIndex, room);
        return room;
    }

    /// <summary>The connection from the room at <paramref name="orderIndex"/> to the next one.</summary>
    public ConnectionKind ConnectionAfter(int orderIndex)
    {
        Room from = _ordered[orderIndex];
        Room to = _ordered[orderIndex + 1];

        // Heroes leave a room on its facing side. A hallway only works if the
        // next room is the neighbouring cell on that side AND faces the same
        // way, so they walk into its travel half and through its door.
        bool hallway = to.Position == from.Position.Step(from.Facing) && to.Facing == from.Facing;
        return hallway ? ConnectionKind.Hallway : ConnectionKind.Portal;
    }
}
