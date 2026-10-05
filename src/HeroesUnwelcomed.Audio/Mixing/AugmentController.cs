using HeroesUnwelcomed.Audio.Timing;

namespace HeroesUnwelcomed.Audio.Mixing;

/// <summary>
/// One kind of musical reaction, e.g. "a hero died": which stems to cut and
/// how deeply, how long to hold, and how it comes back. Loaded from data/audio/augments.json.
/// </summary>
/// <param name="Stems">Stem name → cut depth (1 = fully silent, 0.5 = half volume).</param>
/// <param name="StartGrid">The cut lands on the next multiple of this many beats (0.5 = next half-beat).</param>
/// <param name="HoldBeats">Minimum time the cut lasts before it may return.</param>
/// <param name="ReturnOnBar">If true, the music returns exactly on a bar line (after the hold).</param>
/// <param name="ReleaseBeats">How long the stems take to fade back in.</param>
/// <param name="Stinger">Optional sound played leading into the return (e.g. a drum fill).</param>
/// <param name="StingerLeadBeats">How many beats before the return the stinger starts.</param>
public sealed record AugmentDefinition(
    IReadOnlyDictionary<string, float> Stems,
    double StartGrid = 0.5,
    double HoldBeats = 2,
    bool ReturnOnBar = true,
    double ReleaseBeats = 0.5,
    string? Stinger = null,
    double StingerLeadBeats = 1);

/// <summary>A placed augment: when it starts and ends, and whose influence scales it.</summary>
public sealed record ActiveAugment(int SourceId, AugmentDefinition Definition, double StartBeat, double ReturnBeat);

/// <summary>
/// Turns many simultaneous reactions (from many parties) into one volume per stem.
/// </summary>
/// <remarks>
/// <para>
/// <b>Influence.</b> Every source (a party) has an influence between 0 and 1,
/// worked out by the game from the camera: zoomed in and centred on a party →
/// close to 1; zoomed out or far away → close to 0. A source's reactions are
/// scaled by it, so the party you're watching shapes the music and distant
/// ones barely touch it.
/// </para>
/// <para>
/// <b>Strongest wins.</b> For each stem, the deepest cut currently requested
/// (depth × influence × envelope) is used. Two reactions don't add up into
/// something deeper than either asked for.
/// </para>
/// <para>
/// <b>On the beat.</b> Each reaction has a start beat and a return beat on the
/// music's grid. The mixer asks for volumes at its render position, so cuts and
/// returns land on those beats (to within a few milliseconds).
/// </para>
/// </remarks>
public sealed class AugmentController
{
    private readonly Dictionary<int, float> _influence = [];
    private readonly List<ActiveAugment> _active = [];
    private readonly Dictionary<string, float> _multipliers = [];

    /// <summary>False in "Global" music mode: party reactions are ignored and the track plays untouched.</summary>
    public bool Enabled { get; set; } = true;

    public IReadOnlyList<ActiveAugment> Active => _active;
    public IReadOnlyDictionary<int, float> Influence => _influence;

    public void SetInfluence(int sourceId, float influence) => _influence[sourceId] = Math.Clamp(influence, 0f, 1f);

    public void RemoveSource(int sourceId) => _influence.Remove(sourceId);

    public void Clear()
    {
        _influence.Clear();
        _active.Clear();
    }

    /// <summary>
    /// Starts a reaction for <paramref name="sourceId"/>. It lands on the first grid line at
    /// or after <paramref name="earliestBeat"/>, holds, then returns (on a bar line if asked).
    /// </summary>
    /// <returns>The placed augment, so the caller can schedule its stinger.</returns>
    public ActiveAugment Add(int sourceId, AugmentDefinition definition, double earliestBeat, int beatsPerBar)
    {
        double start = Quantize.NextMultiple(earliestBeat, definition.StartGrid);
        double earliestReturn = start + definition.HoldBeats;
        double returnBeat = definition.ReturnOnBar ? Quantize.NextMultiple(earliestReturn, beatsPerBar) : earliestReturn;

        var augment = new ActiveAugment(sourceId, definition, start, returnBeat);
        _active.Add(augment);
        return augment;
    }

    /// <summary>Volume multiplier per stem at <paramref name="beat"/> (1 = untouched). Stems not mentioned are untouched.</summary>
    public IReadOnlyDictionary<string, float> Multipliers(double beat)
    {
        _multipliers.Clear();
        _active.RemoveAll(a => beat > a.ReturnBeat + a.Definition.ReleaseBeats);
        if (!Enabled)
        {
            return _multipliers;
        }

        foreach (ActiveAugment augment in _active)
        {
            float envelope = Envelope(augment, beat);
            if (envelope <= 0f) continue;

            float influence = _influence.GetValueOrDefault(augment.SourceId);
            foreach (var (stem, depth) in augment.Definition.Stems)
            {
                float cut = depth * influence * envelope;
                float current = 1f - _multipliers.GetValueOrDefault(stem, 1f);
                if (cut > current)
                {
                    _multipliers[stem] = 1f - cut; // the strongest cut wins
                }
            }
        }
        return _multipliers;
    }

    // 0 before the start, 1 while held, then fading to 0 over the release after the return beat.
    private static float Envelope(ActiveAugment augment, double beat)
    {
        if (beat < augment.StartBeat) return 0f;
        if (beat < augment.ReturnBeat) return 1f;
        double release = augment.Definition.ReleaseBeats;
        if (release <= 0) return 0f;
        return (float)Math.Clamp(1 - (beat - augment.ReturnBeat) / release, 0, 1);
    }
}
