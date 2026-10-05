using System.Globalization;
using System.Text.Json;
using HeroesUnwelcomed.Game.Data;
using Microsoft.Xna.Framework;

namespace HeroesUnwelcomed.Game.Animation;

/// <summary>Which clock an animation follows.</summary>
public enum AnimationClock
{
    /// <summary>The music beat: keeps moving while paused. Used for idles and ambient loops.</summary>
    Music,

    /// <summary>The game beat: freezes while paused. Used for actions (walk, attack, death).</summary>
    Sim,
}

/// <summary>A point on a playback-speed curve: from <paramref name="Beat"/> beats after the clip starts, play at <paramref name="Rate"/>× speed.</summary>
public readonly record struct RatePoint(double Beat, double Rate);

/// <summary>One animation of one entity, e.g. Vampire's "Attack".</summary>
public sealed record ClipDefinition(
    string Name,
    int Frames,
    double BeatsPerFrame,
    bool Loop,
    AnimationClock Clock,
    string? Then,
    IReadOnlyList<RatePoint> Rate)
{
    /// <summary>Length in beats when played at normal speed.</summary>
    public double LengthBeats => Frames * BeatsPerFrame;

    /// <summary>
    /// Playback speed <paramref name="elapsedBeats"/> after the clip started,
    /// read off the rate curve with straight lines between its points.
    /// No curve means always 1×.
    /// </summary>
    public double RateAt(double elapsedBeats)
    {
        if (Rate.Count == 0) return 1.0;
        if (elapsedBeats <= Rate[0].Beat) return Rate[0].Rate;

        for (int i = 1; i < Rate.Count; i++)
        {
            if (elapsedBeats <= Rate[i].Beat)
            {
                RatePoint a = Rate[i - 1];
                RatePoint b = Rate[i];
                double t = (elapsedBeats - a.Beat) / (b.Beat - a.Beat);
                return a.Rate + (b.Rate - a.Rate) * t;
            }
        }
        return Rate[^1].Rate;
    }
}

/// <summary>Something that animates (a hero type, a monster, a trap) and its clips.</summary>
public sealed record EntityDefinition(string Name, Color Color, Point Size, IReadOnlyList<ClipDefinition> Clips)
{
    public const string IdleClip = "Idle";

    public ClipDefinition Clip(string name) =>
        Clips.FirstOrDefault(c => c.Name == name)
        ?? throw new ArgumentException($"{Name} has no animation '{name}'.");

    public bool HasClip(string name) => Clips.Any(c => c.Name == name);

    public ClipDefinition Idle => Clip(IdleClip);
}

/// <summary>
/// Every entity's animations, loaded from data/animations/placeholders.json.
/// </summary>
/// <remarks>
/// The file is read element by element (rather than straight into classes) so
/// the clip order in the file is kept and every mistake gets a clear message
/// naming the entity and clip, which matters when you're editing it live.
/// </remarks>
public sealed class AnimationLibrary
{
    public const string FilePath = "animations/placeholders.json";

    private AnimationLibrary(IReadOnlyList<EntityDefinition> entities)
    {
        Entities = entities;
    }

    public IReadOnlyList<EntityDefinition> Entities { get; }

    public EntityDefinition Entity(string name) =>
        Entities.FirstOrDefault(e => e.Name == name)
        ?? throw new ArgumentException($"No entity named '{name}' in {FilePath}.");

    public static AnimationLibrary Load(DataSource data) => Parse(data.ReadText(FilePath));

    public static AnimationLibrary Parse(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        var entities = new List<EntityDefinition>();
        foreach (JsonProperty entity in document.RootElement.GetProperty("entities").EnumerateObject())
        {
            entities.Add(ParseEntity(entity.Name, entity.Value));
        }
        return new AnimationLibrary(entities);
    }

    private static EntityDefinition ParseEntity(string name, JsonElement element)
    {
        Color color = ParseColor(name, element.GetProperty("color").GetString());
        JsonElement size = element.GetProperty("size");
        var clips = new List<ClipDefinition>();

        foreach (JsonProperty clip in element.GetProperty("clips").EnumerateObject())
        {
            clips.Add(ParseClip(name, clip.Name, clip.Value));
        }

        var definition = new EntityDefinition(name, color, new Point(size[0].GetInt32(), size[1].GetInt32()), clips);

        if (!definition.HasClip(EntityDefinition.IdleClip))
        {
            throw new InvalidDataException($"{name}: every entity needs an 'Idle' clip.");
        }
        foreach (ClipDefinition clip in clips.Where(c => c.Then is not null && !definition.HasClip(c.Then)))
        {
            throw new InvalidDataException($"{name}.{clip.Name}: 'then' points at '{clip.Then}', which doesn't exist.");
        }

        return definition;
    }

    private static ClipDefinition ParseClip(string entity, string name, JsonElement element)
    {
        int frames = element.TryGetProperty("frames", out var f) ? f.GetInt32() : 3;
        double beatsPerFrame = element.TryGetProperty("beatsPerFrame", out var b) ? b.GetDouble() : 1.0;
        bool loop = !element.TryGetProperty("loop", out var l) || l.GetBoolean();
        string clockText = element.TryGetProperty("clock", out var c) ? c.GetString() ?? "music" : "music";
        string? then = element.TryGetProperty("then", out var t) ? t.GetString() : null;

        if (frames < 1) throw new InvalidDataException($"{entity}.{name}: frames must be at least 1.");
        if (beatsPerFrame <= 0) throw new InvalidDataException($"{entity}.{name}: beatsPerFrame must be above 0.");
        if (!Enum.TryParse(clockText, ignoreCase: true, out AnimationClock clock))
        {
            throw new InvalidDataException($"{entity}.{name}: clock must be 'music' or 'sim', not '{clockText}'.");
        }

        var rate = new List<RatePoint>();
        if (element.TryGetProperty("rate", out var curve))
        {
            foreach (JsonElement point in curve.EnumerateArray())
            {
                rate.Add(new RatePoint(point[0].GetDouble(), point[1].GetDouble()));
            }
            if (rate.Zip(rate.Skip(1)).Any(pair => pair.Second.Beat <= pair.First.Beat))
            {
                throw new InvalidDataException($"{entity}.{name}: rate points must be in increasing beat order.");
            }
        }

        return new ClipDefinition(name, frames, beatsPerFrame, loop, clock, then, rate);
    }

    private static Color ParseColor(string entity, string? hex)
    {
        if (hex is not { Length: 7 } || hex[0] != '#'
            || !uint.TryParse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgb))
        {
            throw new InvalidDataException($"{entity}: color must look like \"#3D7BD9\", got \"{hex}\".");
        }
        return new Color((int)(rgb >> 16) & 0xFF, (int)(rgb >> 8) & 0xFF, (int)rgb & 0xFF);
    }
}
