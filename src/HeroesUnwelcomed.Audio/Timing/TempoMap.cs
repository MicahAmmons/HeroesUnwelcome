namespace HeroesUnwelcomed.Audio.Timing;

/// <summary>
/// One stretch of time with a constant tempo and game speed.
/// </summary>
/// <param name="StartFrame">The audio frame where this stretch begins (one frame = one sample per channel).</param>
/// <param name="StartBeat">The music beat at <paramref name="StartFrame"/>.</param>
/// <param name="StartSimBeat">The game-logic ("sim") beat at <paramref name="StartFrame"/>.</param>
/// <param name="Bpm">Music tempo in beats per minute.</param>
/// <param name="SimRate">Sim beats per music beat: 0 = paused, 1 = in step with the music, 2 = double time.</param>
public readonly record struct TempoSegment(long StartFrame, double StartBeat, double StartSimBeat, double Bpm, double SimRate);

/// <summary>
/// Converts between audio time (sample frames) and musical time (beats), even
/// though the tempo changes during play.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why frames?</b> The audio hardware consumes samples at an exact rate
/// (48,000 frames per second), so the count of frames played is the most
/// accurate clock the computer has. Everything in the game hangs off it.
/// </para>
/// <para>
/// <b>Why segments?</b> With a constant tempo, beat = frame × bpm / (60 × sampleRate).
/// When the tempo changes we can't use one formula for all time, so we keep a
/// list of segments. Each remembers where it started (in frames AND beats) and
/// its tempo. To find the beat at any frame, find its segment and measure from
/// that segment's start. This is "piecewise linear": a straight line per segment.
/// </para>
/// <para>
/// <b>Two kinds of beat.</b> The <i>music beat</i> always moves while music
/// plays (even when paused, the calm track keeps time). The <i>sim beat</i> is
/// game time: it advances at <see cref="TempoSegment.SimRate"/> sim beats per
/// music beat, and stops completely while paused.
/// </para>
/// </remarks>
public sealed class TempoMap
{
    private readonly List<TempoSegment> _segments = [];

    public TempoMap(int sampleRate, double bpm, double simRate)
    {
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (bpm <= 0) throw new ArgumentOutOfRangeException(nameof(bpm));

        SampleRate = sampleRate;
        _segments.Add(new TempoSegment(0, 0, 0, bpm, simRate));
    }

    public int SampleRate { get; }

    /// <summary>The newest segment: the tempo and game speed in effect at the end of the timeline.</summary>
    public TempoSegment Latest => _segments[^1];

    public IReadOnlyList<TempoSegment> Segments => _segments;

    /// <summary>Audio frames per beat at a given tempo, e.g. 28,800 at 100 bpm and 48 kHz.</summary>
    public double FramesPerBeat(double bpm) => 60.0 * SampleRate / bpm;

    public double BeatAt(double frame)
    {
        TempoSegment segment = SegmentAt(frame);
        return segment.StartBeat + BeatsSinceStart(segment, frame);
    }

    public double SimBeatAt(double frame)
    {
        TempoSegment segment = SegmentAt(frame);
        return segment.StartSimBeat + BeatsSinceStart(segment, frame) * segment.SimRate;
    }

    /// <summary>
    /// Starts a new segment at <paramref name="frame"/> with a new tempo and/or game speed.
    /// Anything not given carries over from the previous segment.
    /// </summary>
    /// <param name="snapBeat">
    /// Optional exact beat number for the new segment's start. A change scheduled
    /// "on beat 12" happens on the first frame at or after beat 12, which is a tiny
    /// fraction past it; snapping keeps the beat count perfectly whole.
    /// </param>
    public void Change(long frame, double? bpm = null, double? simRate = null, double? snapBeat = null)
    {
        TempoSegment latest = Latest;
        if (frame < latest.StartFrame)
        {
            throw new ArgumentOutOfRangeException(nameof(frame), "Tempo changes can only be added at or after the latest segment.");
        }

        var next = new TempoSegment(
            frame,
            snapBeat ?? BeatAt(frame),
            SimBeatAt(frame),
            bpm ?? latest.Bpm,
            simRate ?? latest.SimRate);

        // Two changes on the same frame: the second replaces the first.
        if (frame == latest.StartFrame)
        {
            _segments[^1] = next with { StartBeat = snapBeat ?? latest.StartBeat, StartSimBeat = latest.StartSimBeat };
        }
        else
        {
            _segments.Add(next);
        }
    }

    /// <summary>
    /// Drops segments that end before <paramref name="frame"/>, so the list
    /// doesn't grow forever. Lookups at or after that frame still work.
    /// </summary>
    public void ForgetBefore(double frame)
    {
        int keepFrom = 0;
        for (int i = 1; i < _segments.Count; i++)
        {
            if (_segments[i].StartFrame <= frame)
            {
                keepFrom = i;
            }
        }
        if (keepFrom > 0)
        {
            _segments.RemoveRange(0, keepFrom);
        }
    }

    private TempoSegment SegmentAt(double frame)
    {
        // Searching from the end: almost every lookup is for "now", which is in
        // the newest segment, so this usually finishes on the first check.
        for (int i = _segments.Count - 1; i > 0; i--)
        {
            if (_segments[i].StartFrame <= frame)
            {
                return _segments[i];
            }
        }
        return _segments[0];
    }

    private double BeatsSinceStart(TempoSegment segment, double frame) =>
        (frame - segment.StartFrame) * segment.Bpm / (60.0 * SampleRate);
}
