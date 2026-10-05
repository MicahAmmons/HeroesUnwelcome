using System.Text.Json;
using System.Text.Json.Serialization;

namespace HeroesUnwelcomed.Core.Definitions;

/// <summary>
/// All the fixed game data a run needs: room kinds, tuning numbers and hero
/// generation. Built from JSON text. Core never touches files itself, so the
/// game, tests and SimRunner each decide where the text comes from.
/// </summary>
public sealed class GameDefinitions
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(), new GridPointConverter() },
    };

    private readonly Dictionary<string, RoomKind> _rooms;

    public GameDefinitions(IEnumerable<RoomKind> rooms, Tuning tuning, HeroGeneration heroes)
    {
        _rooms = [];
        foreach (RoomKind room in rooms)
        {
            if (!_rooms.TryAdd(room.Id, room))
            {
                throw new InvalidDataException($"Room kind '{room.Id}' is defined twice.");
            }
            if (room.IsChallenge && room.Difficulty <= 0)
            {
                throw new InvalidDataException($"Room kind '{room.Id}' needs a difficulty above 0.");
            }
        }
        Tuning = tuning;
        Heroes = heroes;
    }

    public IReadOnlyCollection<RoomKind> RoomKinds => _rooms.Values;
    public Tuning Tuning { get; }
    public HeroGeneration Heroes { get; }

    public RoomKind Room(string id) =>
        _rooms.TryGetValue(id, out var kind) ? kind : throw new KeyNotFoundException($"No room kind named '{id}'.");

    public bool HasRoom(string id) => _rooms.ContainsKey(id);

    public static GameDefinitions Parse(string roomsJson, string tuningJson, string heroesJson)
    {
        var rooms = Deserialize<RoomsFile>(roomsJson, "rooms").Rooms;
        var tuning = Deserialize<Tuning>(tuningJson, "tuning");
        var heroes = Deserialize<HeroGeneration>(heroesJson, "heroes");
        return new GameDefinitions(rooms, tuning, heroes);
    }

    public static RunSetup ParseSetup(string json) => Deserialize<RunSetup>(json, "run setup");

    private static T Deserialize<T>(string json, string what)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                ?? throw new InvalidDataException($"The {what} data is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"The {what} data has a problem: {ex.Message}", ex);
        }
    }

    private sealed record RoomsFile(IReadOnlyList<RoomKind> Rooms);

    // Lets JSON write grid positions compactly as [x, y].
    private sealed class GridPointConverter : JsonConverter<GridPoint>
    {
        public override GridPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            int[] xy = JsonSerializer.Deserialize<int[]>(ref reader, options) ?? [];
            if (xy.Length != 2)
            {
                throw new JsonException("A grid position must look like [x, y].");
            }
            return new GridPoint(xy[0], xy[1]);
        }

        public override void Write(Utf8JsonWriter writer, GridPoint value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, new[] { value.X, value.Y }, options);
    }
}
