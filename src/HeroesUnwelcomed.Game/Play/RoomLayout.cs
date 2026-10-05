using System.Text.Json;
using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Game.Data;
using Microsoft.Xna.Framework;

namespace HeroesUnwelcomed.Game.Play;

/// <summary>
/// The anchor points inside a room, as fractions of the cell width along the
/// direction of travel (0 = entry side, 1 = exit side).
/// </summary>
public sealed record RoomAnchors(float LineFront, float HeroSpot, float Encounter, float Door, float EntryPortal, float ExitPortal);

/// <summary>
/// Where every choreography step starts and ends: the line, the hero spot,
/// the encounter, doors and portals. Loaded from data/game/layout.json,
/// with optional per-room-kind overrides.
/// </summary>
/// <remarks>
/// Because positions are fractions "along the way through", the same numbers
/// work for rooms facing either way. <see cref="WorldLayout.ToWorld"/> mirrors them.
/// </remarks>
public sealed class RoomLayout
{
    public const string FilePath = "game/layout.json";

    private readonly RoomAnchors _defaults;
    private readonly Dictionary<string, RoomAnchors> _overrides;
    private readonly Dictionary<RoomCategory, string> _actionClips;

    private RoomLayout(RoomAnchors defaults, Dictionary<string, RoomAnchors> overrides, float spacing, Dictionary<RoomCategory, string> actionClips)
    {
        _defaults = defaults;
        _overrides = overrides;
        Spacing = spacing * WorldLayout.CellWidth;
        _actionClips = actionClips;
    }

    /// <summary>Distance between heroes in a line, in world units.</summary>
    public float Spacing { get; }

    public RoomAnchors For(string roomKind) => _overrides.GetValueOrDefault(roomKind, _defaults);

    /// <summary>The hero animation used for the Action step in this kind of room (Fight, Disarm...).</summary>
    public string ActionClip(RoomCategory category) => _actionClips.GetValueOrDefault(category, "Fight");

    // ---- World positions -----------------------------------------------------

    public Vector2 At(Room room, float fraction, float behind = 0) =>
        WorldLayout.ToWorld(room.Position, room.Facing, fraction * WorldLayout.CellWidth - behind);

    /// <summary>Place <paramref name="index"/> in the waiting line (0 = front, at the border).</summary>
    public Vector2 LineSlot(Room room, int index) => At(room, For(room.Kind.Id).LineFront, index * Spacing);

    /// <summary>The front hero's spot facing the encounter.</summary>
    public Vector2 HeroSpot(Room room) => At(room, For(room.Kind.Id).HeroSpot);

    /// <summary>Lined up behind the hero spot (where the party gathers behind a victor).</summary>
    public Vector2 BehindHeroSpot(Room room, int index) => At(room, For(room.Kind.Id).HeroSpot, index * Spacing);

    public Vector2 Encounter(Room room) => At(room, For(room.Kind.Id).Encounter);

    public Vector2 Door(Room room) => At(room, For(room.Kind.Id).Door);

    public Vector2 EntryPortal(Room room) => At(room, For(room.Kind.Id).EntryPortal);

    public Vector2 ExitPortal(Room room) => At(room, For(room.Kind.Id).ExitPortal);

    /// <summary>The exit edge of the cell (where a hallway continues into the next room).</summary>
    public static Vector2 ExitEdge(Room room) => WorldLayout.ToWorld(room.Position, room.Facing, WorldLayout.CellWidth);

    // ---- Loading -------------------------------------------------------------

    public static RoomLayout Load(DataSource data) => Parse(data.ReadText(FilePath));

    public static RoomLayout Parse(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });
        JsonElement root = document.RootElement;

        RoomAnchors defaults = ReadAnchors(root.GetProperty("default"), fallback: null);
        float spacing = root.TryGetProperty("spacing", out var s) ? s.GetSingle() : 0.13f;

        var overrides = new Dictionary<string, RoomAnchors>();
        if (root.TryGetProperty("rooms", out var rooms))
        {
            foreach (JsonProperty room in rooms.EnumerateObject())
            {
                overrides[room.Name] = ReadAnchors(room.Value, defaults);
            }
        }

        var actionClips = new Dictionary<RoomCategory, string>();
        if (root.TryGetProperty("actionClips", out var clips))
        {
            foreach (JsonProperty clip in clips.EnumerateObject())
            {
                if (!Enum.TryParse(clip.Name, ignoreCase: true, out RoomCategory category))
                {
                    throw new InvalidDataException($"{FilePath}: unknown room category '{clip.Name}' in actionClips.");
                }
                actionClips[category] = clip.Value.GetString() ?? "Fight";
            }
        }

        return new RoomLayout(defaults, overrides, spacing, actionClips);
    }

    // Reads anchor values; anything missing comes from the fallback (the defaults).
    private static RoomAnchors ReadAnchors(JsonElement element, RoomAnchors? fallback)
    {
        float Read(string name, float? otherwise)
        {
            if (element.TryGetProperty(name, out var value)) return value.GetSingle();
            return otherwise ?? throw new InvalidDataException($"{FilePath}: the default layout needs '{name}'.");
        }

        return new RoomAnchors(
            Read("lineFront", fallback?.LineFront),
            Read("heroSpot", fallback?.HeroSpot),
            Read("encounter", fallback?.Encounter),
            Read("door", fallback?.Door),
            Read("entryPortal", fallback?.EntryPortal),
            Read("exitPortal", fallback?.ExitPortal));
    }
}
