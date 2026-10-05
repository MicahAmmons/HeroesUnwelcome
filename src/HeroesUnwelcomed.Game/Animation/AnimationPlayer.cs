using HeroesUnwelcomed.Game.Sound;

namespace HeroesUnwelcomed.Game.Animation;

/// <summary>
/// Plays one entity's animations in time with the beat.
/// </summary>
/// <remarks>
/// <para>
/// Animation time is counted in <b>beats</b>, never seconds. That's what ties
/// the animations to the music: at a faster tempo, beats are shorter, so
/// everything moves faster together.
/// </para>
/// <para>
/// <b>Looping music clips</b> (idles) don't remember when they started. Their
/// frame comes straight from the global music beat, so every idle on screen
/// steps in unison and lands on the beat, however they got there.
/// </para>
/// <para>
/// <b>Everything else</b> keeps its own progress, advanced each frame by beats
/// elapsed × the clip's playback rate (from its rate curve, e.g. Fear: frozen,
/// then slow, then full speed).
/// </para>
/// <para>
/// <b>Pause.</b> Sim clips gain no progress while paused (sim beats stop), so
/// they resume exactly where they were. Meanwhile we *show* the Idle clip, so
/// the scene doesn't look frozen.
/// </para>
/// </remarks>
public sealed class AnimationPlayer
{
    private ClipDefinition _clip;
    private double _progressBeats;
    private double _elapsedBeats;

    public AnimationPlayer(EntityDefinition entity)
    {
        Entity = entity;
        _clip = entity.Idle;
    }

    public EntityDefinition Entity { get; private set; }

    /// <summary>The clip that's logically playing.</summary>
    public ClipDefinition Clip => _clip;

    /// <summary>The clip being drawn (Idle stands in for paused sim clips).</summary>
    public ClipDefinition DisplayClip { get; private set; } = null!;

    /// <summary>Which frame of <see cref="DisplayClip"/> to draw.</summary>
    public int Frame { get; private set; }

    /// <summary>Progress through the current clip, 0..1 (for debugging displays).</summary>
    public double Progress => _clip.Loop ? 0 : Math.Clamp(_progressBeats / _clip.LengthBeats, 0, 1);

    public void Play(string clipName)
    {
        _clip = Entity.Clip(clipName);
        _progressBeats = 0;
        _elapsedBeats = 0;
    }

    /// <summary>Swaps in new definitions (after a live data reload), keeping the current clip if it still exists.</summary>
    public void Rebind(EntityDefinition entity)
    {
        Entity = entity;
        _clip = entity.HasClip(_clip.Name) ? entity.Clip(_clip.Name) : entity.Idle;
    }

    public void Update(BeatTime time)
    {
        bool musicLockedLoop = _clip.Loop && _clip.Clock == AnimationClock.Music;
        if (!musicLockedLoop)
        {
            double delta = _clip.Clock == AnimationClock.Music ? time.MusicDelta : time.SimDelta;
            _progressBeats += delta * _clip.RateAt(_elapsedBeats);
            _elapsedBeats += delta;
        }

        if (!_clip.Loop && _progressBeats >= _clip.LengthBeats)
        {
            if (_clip.Then is { } next)
            {
                Play(next);
            }
            else
            {
                _progressBeats = _clip.LengthBeats; // no follow-up: hold the last frame (e.g. Death)
            }
        }

        bool showIdle = time.Paused && _clip.Clock == AnimationClock.Sim;
        DisplayClip = showIdle ? Entity.Idle : _clip;
        Frame = FrameOf(DisplayClip, showIdle ? 0 : _progressBeats, time.MusicBeat);
    }

    private static int FrameOf(ClipDefinition clip, double progressBeats, double musicBeat)
    {
        if (clip.Loop && clip.Clock == AnimationClock.Music)
        {
            return PositiveModulo((int)Math.Floor(musicBeat / clip.BeatsPerFrame), clip.Frames);
        }

        int frame = (int)Math.Floor(progressBeats / clip.BeatsPerFrame);
        return clip.Loop
            ? PositiveModulo(frame, clip.Frames)
            : Math.Clamp(frame, 0, clip.Frames - 1);
    }

    private static int PositiveModulo(int value, int modulus) => ((value % modulus) + modulus) % modulus;
}
