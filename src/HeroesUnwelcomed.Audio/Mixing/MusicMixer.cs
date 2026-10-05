using HeroesUnwelcomed.Audio.Music;
using HeroesUnwelcomed.Audio.Timing;

namespace HeroesUnwelcomed.Audio.Mixing;

/// <summary>
/// Produces the final stereo music, sample by sample, and keeps the beat clock.
/// </summary>
/// <remarks>
/// <para>
/// <b>How rendering works.</b> The speakers repeatedly ask for the next small
/// buffer of audio (about 21 ms). For each frame in it the mixer:
/// <list type="number">
///   <item>works out the current beat from the <see cref="TempoMap"/>,</item>
///   <item>applies any scheduled change whose beat has arrived (sample-accurate),</item>
///   <item>finds that beat's position inside the looping stems,</item>
///   <item>adds every stem's sample × its volume, split left/right by its pan.</item>
/// </list>
/// </para>
/// <para>
/// <b>Render head vs. what you hear.</b> Audio is prepared a little ahead of
/// playback (a few buffers) so the sound never runs dry. <see cref="RenderFrame"/>
/// is where the mixer is writing; the audible position is slightly behind it.
/// That's why changes are scheduled for a future beat rather than applied "now":
/// "now" for the mixer is already a few milliseconds in the future.
/// </para>
/// <para>
/// <b>Muting keeps time.</b> A muted stem's position keeps advancing with the
/// beat, because the position comes from the beat, not from the stem. Bringing
/// it back later lands exactly where it would have been, always in time.
/// </para>
/// <para>
/// <b>Two layers of stem volume.</b> Each stem's own volume (set by
/// <see cref="ScheduleStemGain"/>, e.g. debug mutes) is multiplied by the
/// <see cref="Augments"/> layer (party reactions), which is re-read every few
/// milliseconds at the render position so reactions land on their beats.
/// </para>
/// <para>
/// <b>Sound effects</b> (<see cref="PlaySound"/>) are added on top of the music,
/// unaffected by stem cuts, each with its own volume and left/right position.
/// </para>
/// </remarks>
public sealed class MusicMixer
{
    // Shortest allowed volume glide (5 ms) so mutes never click.
    private const double MinimumRampSeconds = 0.005;

    // How often (in frames) the augment layer is re-read: 128 frames ≈ 2.7 ms.
    private const int AugmentBlockFrames = 128;

    // Smoothing for augment volume changes: about an 8 ms glide, so cuts never click.
    private const float AugmentSmoothing = 0.0026f;

    private readonly MusicTrack _track;
    private readonly Dictionary<string, StemChannel> _channels;
    private readonly StemChannel[] _channelList;
    private readonly List<ScheduledChange> _schedule = [];
    private readonly List<Voice> _voices = [];
    private MusicVersion _version;

    public MusicMixer(MusicTrack track, string startVersion, double startSimRate)
    {
        _track = track;
        _version = GetVersion(startVersion);
        Tempo = new TempoMap(_version.SampleRate, _version.Bpm, startSimRate);

        _channels = track.StemNames.ToDictionary(name => name, name => new StemChannel(name));
        _channelList = [.. _channels.Values];
        AttachStemBuffers();
    }

    public TempoMap Tempo { get; }
    public int SampleRate => Tempo.SampleRate;

    /// <summary>Next frame the mixer will write (ahead of what's audible).</summary>
    public long RenderFrame { get; private set; }

    /// <summary>Beat at the render head.</summary>
    public double RenderBeat => Tempo.BeatAt(RenderFrame);

    public MusicVersion CurrentVersion => _version;
    public IReadOnlyList<StemChannel> Channels => _channelList;
    public float MasterGain { get; set; } = 0.7f;

    /// <summary>Volume of sound effects relative to the music.</summary>
    public float SoundGain { get; set; } = 1f;

    /// <summary>Party reactions layered on the stems (see <see cref="AugmentController"/>).</summary>
    public AugmentController Augments { get; } = new();

    /// <summary>Sound effects currently playing or waiting for their beat.</summary>
    public int ActiveSounds => _voices.Count;

    /// <summary>The augment multiplier currently applied to a stem (for the debug panel).</summary>
    public float AugmentGainOf(string stem) => Channel(stem).AugmentGain;

    /// <summary>Changes not yet applied (for the debug panel).</summary>
    public int PendingChanges => _schedule.Count;

    public StemChannel Channel(string stem) =>
        _channels.TryGetValue(stem, out var channel)
            ? channel
            : throw new ArgumentException($"Track '{_track.Name}' has no stem '{stem}'.");

    /// <summary>The next beat-grid line the mixer hasn't rendered yet (grid 1 = beat, 4 = bar).</summary>
    public double NextGridBeat(double grid) => Quantize.NextMultiple(RenderBeat, grid);

    /// <summary>Changes game speed from the render head onward, without waiting for a beat.</summary>
    public void SetSimRateNow(double simRate) => Tempo.Change(RenderFrame, simRate: simRate);

    /// <summary>Switches to another version (tempo + arrangement) exactly on <paramref name="atBeat"/>.</summary>
    public void ScheduleVersion(string version, double atBeat, double simRate)
    {
        GetVersion(version); // fail now, not mid-render, if the name is wrong
        AddToSchedule(new ScheduledChange(atBeat, ChangeKind.Version, version, 0f, 0, simRate));
    }

    /// <summary>Glides a stem to <paramref name="gain"/>, starting exactly on <paramref name="atBeat"/>.</summary>
    public void ScheduleStemGain(string stem, float gain, double atBeat, double rampBeats = 0)
    {
        Channel(stem); // validate the name now
        AddToSchedule(new ScheduledChange(atBeat, ChangeKind.Gain, stem, gain, rampBeats, 0));
    }

    /// <summary>
    /// Plays a one-off sound on top of the music.
    /// </summary>
    /// <param name="samples">Mono samples at the mixer's sample rate.</param>
    /// <param name="gain">Volume (1 = as recorded).</param>
    /// <param name="pan">-1 = left, 0 = centre, 1 = right.</param>
    /// <param name="atBeat">Start exactly on this music beat, or at once if null (or already past).</param>
    public void PlaySound(float[] samples, float gain, float pan, double? atBeat = null)
    {
        float angle = (Math.Clamp(pan, -1f, 1f) + 1f) * MathF.PI / 4f;
        _voices.Add(new Voice(samples, gain, MathF.Cos(angle), MathF.Sin(angle), atBeat));
    }

    /// <summary>Fills <paramref name="output"/> with interleaved stereo samples (L, R, L, R...).</summary>
    public void Render(Span<float> output)
    {
        int frames = output.Length / 2;
        for (int i = 0; i < frames; i++)
        {
            double beat = Tempo.BeatAt(RenderFrame);

            if (i % AugmentBlockFrames == 0)
            {
                ReadAugments(beat);
            }

            while (_schedule.Count > 0 && _schedule[0].Beat <= beat + 1e-9)
            {
                Apply(_schedule[0]);
                _schedule.RemoveAt(0);
                beat = Tempo.BeatAt(RenderFrame);
            }

            int index = PositionInLoop(beat);
            float left = 0f;
            float right = 0f;

            foreach (StemChannel channel in _channelList)
            {
                float augment = channel.AugmentGain;
                float augmentTarget = channel.AugmentTarget;
                if (augment != augmentTarget)
                {
                    augment += (augmentTarget - augment) * AugmentSmoothing;
                    if (MathF.Abs(augmentTarget - augment) < 1e-4f) augment = augmentTarget;
                    channel.AugmentGain = augment;
                }

                float gain = channel.NextGain() * augment;
                if (gain == 0f || channel.Buffer is not { } buffer)
                {
                    continue;
                }

                float sample = buffer[index] * gain;
                left += sample * channel.LeftGain;
                right += sample * channel.RightGain;
            }

            MixVoices(beat, ref left, ref right);

            output[i * 2] = SoftClip(left * MasterGain);
            output[i * 2 + 1] = SoftClip(right * MasterGain);
            RenderFrame++;
        }

        _voices.RemoveAll(v => v.Finished);
    }

    private void ReadAugments(double beat)
    {
        IReadOnlyDictionary<string, float> multipliers = Augments.Multipliers(beat);
        foreach (StemChannel channel in _channelList)
        {
            channel.AugmentTarget = multipliers.GetValueOrDefault(channel.Name, 1f);
        }
    }

    private void MixVoices(double beat, ref float left, ref float right)
    {
        foreach (Voice voice in _voices)
        {
            if (voice.Finished) continue;
            if (voice.StartBeat is { } start && beat < start - 1e-9) continue; // not its beat yet

            float sample = voice.Samples[voice.Position] * voice.Gain * SoundGain;
            left += sample * voice.Left;
            right += sample * voice.Right;
            voice.Position++;
        }
    }

    private int PositionInLoop(double beat)
    {
        double loopBeat = beat % _version.LoopBeats;
        if (loopBeat < 0)
        {
            loopBeat += _version.LoopBeats;
        }
        int index = (int)(loopBeat * _version.FramesPerBeat);
        return Math.Min(index, _version.LoopFrames - 1);
    }

    private void Apply(ScheduledChange change)
    {
        switch (change.Kind)
        {
            case ChangeKind.Version:
                _version = GetVersion(change.Target);
                Tempo.Change(RenderFrame, bpm: _version.Bpm, simRate: change.SimRate, snapBeat: change.Beat);
                AttachStemBuffers();
                break;

            case ChangeKind.Gain:
                double rampFrames = Math.Max(change.RampBeats * _version.FramesPerBeat, MinimumRampSeconds * SampleRate);
                _channels[change.Target].RampTo(change.Gain, (int)rampFrames);
                break;
        }
    }

    private void AttachStemBuffers()
    {
        foreach (StemChannel channel in _channelList)
        {
            channel.Buffer = _version.Stems.TryGetValue(channel.Name, out var buffer) ? buffer : null;
        }
    }

    // Keep the schedule sorted by beat. Changes on the same beat run in the order they were requested.
    private void AddToSchedule(ScheduledChange change)
    {
        int index = _schedule.FindIndex(existing => existing.Beat > change.Beat);
        _schedule.Insert(index < 0 ? _schedule.Count : index, change);
    }

    private MusicVersion GetVersion(string name) =>
        _track.Versions.TryGetValue(name, out var version)
            ? version
            : throw new ArgumentException($"Track '{_track.Name}' has no version '{name}'.");

    // Gentle limiter: below 0.8 the signal passes untouched; above it, peaks are
    // squeezed smoothly toward 1.0 instead of being chopped flat (which distorts harshly).
    private static float SoftClip(float x)
    {
        float magnitude = MathF.Abs(x);
        if (magnitude <= 0.8f)
        {
            return x;
        }
        float squeezed = 0.8f + 0.2f * MathF.Tanh((magnitude - 0.8f) / 0.2f);
        return MathF.CopySign(squeezed, x);
    }

    private enum ChangeKind
    {
        Version,
        Gain,
    }

    private sealed class Voice(float[] samples, float gain, float left, float right, double? startBeat)
    {
        public float[] Samples { get; } = samples;
        public float Gain { get; } = gain;
        public float Left { get; } = left;
        public float Right { get; } = right;
        public double? StartBeat { get; } = startBeat;
        public int Position { get; set; }
        public bool Finished => Position >= Samples.Length;
    }

    private readonly record struct ScheduledChange(double Beat, ChangeKind Kind, string Target, float Gain, double RampBeats, double SimRate);
}
