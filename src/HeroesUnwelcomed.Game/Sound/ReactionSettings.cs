using System.Text.Json.Serialization;
using HeroesUnwelcomed.Audio.Mixing;
using HeroesUnwelcomed.Game.Data;

namespace HeroesUnwelcomed.Game.Sound;

/// <summary>Whether parties shape the music (Local) or the track plays untouched (Global).</summary>
public enum MusicMode
{
    Local,
    Global,
}

/// <summary>Camera-based influence settings (see <see cref="Proximity"/>).</summary>
public sealed record ProximitySettings(float ZoomedOut = 0.15f, float ZoomedIn = 0.45f, float MinSoundVolume = 0.2f, float PanWidth = 0.8f);

/// <summary>How music and sounds react to party events. Loaded from data/audio/reactions.json.</summary>
public sealed class ReactionSettings
{
    public const string FilePath = "audio/reactions.json";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MusicMode DefaultMode { get; set; } = MusicMode.Local;

    public Dictionary<string, AugmentDefinition> MusicReactions { get; set; } = [];

    public Dictionary<string, string> Sounds { get; set; } = [];

    public ProximitySettings Proximity { get; set; } = new();

    public static ReactionSettings Load(DataSource data) =>
        data.Exists(FilePath) ? data.ReadJson<ReactionSettings>(FilePath) : new ReactionSettings();
}

/// <summary>
/// Turns "where is this party on screen, and how far are we zoomed in" into
/// numbers for the audio: its influence over the music, and the volume and
/// left/right position of its sounds.
/// </summary>
/// <remarks>
/// <para>
/// <b>Zoom factor:</b> 0 when zoomed out past <c>ZoomedOut</c> (the whole dungeon
/// in view: no single party should dominate), rising smoothly to 1 at
/// <c>ZoomedIn</c>. "Smoothly" uses smoothstep, an S-curve, so there's no sudden jump.
/// </para>
/// <para>
/// <b>Centre factor:</b> 1 for a party in the middle of the screen, falling to
/// 0 a little beyond the screen's edge.
/// </para>
/// <para>
/// Influence = zoom factor × centre factor. Panning from one party to another
/// therefore hands the music over gradually.
/// </para>
/// </remarks>
public static class Proximity
{
    /// <summary>
    /// <paramref name="offset"/> is the party's position relative to the screen centre,
    /// as a fraction of half the screen size (so ±1 is the screen edge).
    /// </summary>
    public static float Influence(float zoom, System.Numerics.Vector2 offset, ProximitySettings settings)
    {
        float zoomFactor = SmoothStep(settings.ZoomedOut, settings.ZoomedIn, zoom);
        float centreFactor = Math.Clamp(1f - offset.Length() / 1.2f, 0f, 1f);
        return zoomFactor * centreFactor;
    }

    public static float SoundVolume(float influence, ProximitySettings settings) =>
        settings.MinSoundVolume + (1f - settings.MinSoundVolume) * influence;

    public static float Pan(System.Numerics.Vector2 offset, ProximitySettings settings) =>
        Math.Clamp(offset.X, -1f, 1f) * settings.PanWidth;

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
