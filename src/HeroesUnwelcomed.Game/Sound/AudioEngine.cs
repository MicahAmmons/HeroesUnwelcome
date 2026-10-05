using HeroesUnwelcomed.Audio.Mixing;
using HeroesUnwelcomed.Audio.Music;
using HeroesUnwelcomed.Audio.Timing;
using HeroesUnwelcomed.Game.Data;

namespace HeroesUnwelcomed.Game.Sound;

/// <summary>
/// The game's view of time, taken from what's actually being heard.
/// </summary>
/// <param name="MusicBeat">Beats of music heard so far. Keeps moving while paused.</param>
/// <param name="MusicDelta">Music beats since last frame.</param>
/// <param name="SimBeat">Game-logic beats so far. Stops while paused.</param>
/// <param name="SimDelta">Game beats since last frame.</param>
/// <param name="Paused">True while the game speed is paused.</param>
/// <param name="BeatsPerBar">Beats in a bar of the current music.</param>
public readonly record struct BeatTime(double MusicBeat, double MusicDelta, double SimBeat, double SimDelta, bool Paused, int BeatsPerBar)
{
    public int Bar => (int)Math.Floor(MusicBeat / BeatsPerBar);
    public int BeatInBar => (int)Math.Floor(MusicBeat) % BeatsPerBar;
}

/// <summary>When a scheduled change should happen.</summary>
public enum When
{
    NextBeat,
    NextBar,
}

/// <summary>
/// Runs the music and is the master clock for everything that moves.
/// </summary>
/// <remarks>
/// <para>
/// Each frame: refill the audio queue, then work out which beat the listener
/// is hearing right now. That beat (<see cref="Time"/>) drives every animation,
/// and later the game logic, so sound and visuals can't drift apart.
/// </para>
/// <para>
/// <b>Smoothing.</b> The raw "what's audible" estimate only updates when a
/// buffer finishes (every ~21 ms) and jumps in steps. Animations need a value
/// that moves smoothly every frame, so we advance our own copy by real elapsed
/// time and gently pull it toward the raw estimate (5% of the gap per frame).
/// If they ever disagree badly (e.g. after a stall) we jump straight to the raw value.
/// </para>
/// <para>
/// <b>Speed changes</b> have two parts. The game speed (sim rate) changes almost
/// immediately, so pause feels instant. The music switches to the matching
/// version on the next beat, so it changes musically.
/// </para>
/// </remarks>
public sealed class AudioEngine : IDisposable
{
    public const string MetronomeStem = "click";

    private readonly AudioOutput _output = new();
    private readonly MusicMixer _mixer;
    private readonly SpeedLevel _pauseLevel;
    private readonly SpeedLevel[] _playingLevels;
    private SpeedLevel _lastPlayingLevel;
    private double _smoothedFrame;
    private bool _clockStarted;
    private double _lastMusicBeat;
    private double _lastSimBeat;

    public AudioEngine(IReadOnlyList<SpeedLevel> levels, MusicTrack track)
    {
        Levels = levels;
        _pauseLevel = levels.FirstOrDefault(l => l.IsPause)
            ?? throw new InvalidDataException("speeds.json needs a level with simRate 0 (pause).");
        _playingLevels = levels.Where(l => !l.IsPause).ToArray();
        if (_playingLevels.Length == 0)
        {
            throw new InvalidDataException("speeds.json needs at least one playing (non-pause) level.");
        }

        CurrentLevel = _lastPlayingLevel = _playingLevels[0];
        _mixer = new MusicMixer(track, CurrentLevel.Name, CurrentLevel.SimRate);
        _mixer.Channel(MetronomeStem).RampTo(0f, 0); // metronome starts off
    }

    /// <summary>Builds the engine with generated placeholder music, using data/audio/speeds.json.</summary>
    public static AudioEngine CreateWithPlaceholderMusic(DataSource data)
    {
        var file = data.ReadJson<SpeedsFile>("audio/speeds.json");
        MusicTrack track = PlaceholderMusic.Create(file.Levels, AudioOutput.SampleRate);
        return new AudioEngine(file.Levels, track);
    }

    public IReadOnlyList<SpeedLevel> Levels { get; }

    /// <summary>The speed the player last chose (the music may still be catching up to it).</summary>
    public SpeedLevel CurrentLevel { get; private set; }

    public bool IsPaused => CurrentLevel.IsPause;
    public bool MetronomeOn { get; private set; }

    /// <summary>The current beat as heard. Read this once per frame for animation.</summary>
    public BeatTime Time { get; private set; }

    /// <summary>
    /// Extra delay of the sound driver/speakers that we can't measure, in ms.
    /// Raising it makes visuals happen slightly later to line up with what's heard.
    /// </summary>
    public float OutputLatencyMs { get; set; }

    public MusicMixer Mixer => _mixer;
    public AudioOutput Output => _output;

    public void Update(double deltaSeconds)
    {
        _output.Pump(_mixer, deltaSeconds);
        UpdateClock(deltaSeconds);
    }

    public void SetSpeed(SpeedLevel level)
    {
        if (level == CurrentLevel)
        {
            return;
        }

        if (!level.IsPause)
        {
            _lastPlayingLevel = level;
        }
        CurrentLevel = level;

        _mixer.SetSimRateNow(level.SimRate);
        _mixer.ScheduleVersion(level.Name, _mixer.NextGridBeat(1), level.SimRate);
    }

    public void TogglePause() => SetSpeed(IsPaused ? _lastPlayingLevel : _pauseLevel);

    public void SpeedUp() => StepSpeed(+1);

    public void SpeedDown() => StepSpeed(-1);

    public void SetMetronome(bool on)
    {
        MetronomeOn = on;
        ScheduleStemGain(MetronomeStem, on ? 1f : 0f, When.NextBeat);
    }

    /// <summary>Changes a stem's volume on the next beat or bar. Returns the beat it will happen on.</summary>
    public double ScheduleStemGain(string stem, float gain, When when, double rampBeats = 0)
    {
        double beat = _mixer.NextGridBeat(when == When.NextBar ? _mixer.CurrentVersion.BeatsPerBar : 1);
        _mixer.ScheduleStemGain(stem, gain, beat, rampBeats);
        return beat;
    }

    private void StepSpeed(int direction)
    {
        int index = Array.IndexOf(_playingLevels, _lastPlayingLevel);
        int next = Math.Clamp(index + direction, 0, _playingLevels.Length - 1);
        SetSpeed(_playingLevels[next]);
    }

    private void UpdateClock(double deltaSeconds)
    {
        int rate = AudioOutput.SampleRate;
        double raw = _output.AudibleFrame;

        if (!_clockStarted)
        {
            _smoothedFrame = raw;
            _clockStarted = true;
        }
        else
        {
            double previous = _smoothedFrame;
            _smoothedFrame += deltaSeconds * rate;
            double error = raw - _smoothedFrame;
            _smoothedFrame = Math.Abs(error) > rate * 0.1
                ? raw
                : _smoothedFrame + error * 0.05;
            _smoothedFrame = Math.Max(_smoothedFrame, previous); // time never runs backwards
        }

        double heardFrame = Math.Max(0, _smoothedFrame - OutputLatencyMs / 1000.0 * rate);
        TempoMap tempo = _mixer.Tempo;
        double musicBeat = tempo.BeatAt(heardFrame);
        double simBeat = tempo.SimBeatAt(heardFrame);

        Time = new BeatTime(
            musicBeat,
            Math.Max(0, musicBeat - _lastMusicBeat),
            simBeat,
            Math.Max(0, simBeat - _lastSimBeat),
            IsPaused,
            _mixer.CurrentVersion.BeatsPerBar);

        _lastMusicBeat = musicBeat;
        _lastSimBeat = simBeat;

        // Keep about a second of tempo history behind what's audible; older segments are never needed again.
        tempo.ForgetBefore(heardFrame - rate);
    }

    public void Dispose() => _output.Dispose();

    private sealed class SpeedsFile
    {
        public List<SpeedLevel> Levels { get; set; } = [];
    }
}
