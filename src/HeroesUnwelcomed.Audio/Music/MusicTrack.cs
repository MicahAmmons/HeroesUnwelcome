namespace HeroesUnwelcomed.Audio.Music;

/// <summary>
/// One piece of music, recorded (or generated) once per game speed.
/// </summary>
/// <remarks>
/// The composer delivers a version per speed level (calm for pause, normal, and
/// faster ones). Every version shares the same stem names, so muting "lead"
/// means the same thing whichever version is playing.
/// </remarks>
public sealed class MusicTrack
{
    public MusicTrack(string name, IReadOnlyDictionary<string, MusicVersion> versions)
    {
        if (versions.Count == 0) throw new ArgumentException("A track needs at least one version.", nameof(versions));

        Name = name;
        Versions = versions;
        StemNames = versions.Values.SelectMany(v => v.Stems.Keys).Distinct().ToArray();
    }

    public string Name { get; }
    public IReadOnlyDictionary<string, MusicVersion> Versions { get; }

    /// <summary>Every stem name used by any version (e.g. drums, hats, bass, lead, pad, click).</summary>
    public IReadOnlyList<string> StemNames { get; }
}

/// <summary>
/// One tempo of a track: a short loop split into stems (separate instruments).
/// </summary>
/// <remarks>
/// Each stem is a mono buffer of samples that loops seamlessly every
/// <see cref="LoopBeats"/> beats. Stems in a version are exactly the same length and
/// line up sample-for-sample, which is what lets the mixer mute one and bring it
/// back later exactly in time.
/// </remarks>
public sealed class MusicVersion
{
    public MusicVersion(string name, double bpm, int beatsPerBar, int loopBars, int sampleRate, IReadOnlyDictionary<string, float[]> stems)
    {
        Name = name;
        Bpm = bpm;
        BeatsPerBar = beatsPerBar;
        LoopBars = loopBars;
        SampleRate = sampleRate;
        Stems = stems;
        FramesPerBeat = 60.0 * sampleRate / bpm;
        LoopFrames = (int)Math.Round(LoopBeats * FramesPerBeat);

        foreach (var (stem, buffer) in stems)
        {
            if (buffer.Length != LoopFrames)
            {
                throw new ArgumentException($"Stem '{stem}' in version '{name}' has {buffer.Length} samples; expected {LoopFrames}.");
            }
        }
    }

    public string Name { get; }
    public double Bpm { get; }
    public int BeatsPerBar { get; }
    public int LoopBars { get; }
    public int SampleRate { get; }
    public IReadOnlyDictionary<string, float[]> Stems { get; }
    public double FramesPerBeat { get; }
    public int LoopBeats => LoopBars * BeatsPerBar;
    public int LoopFrames { get; }
}
