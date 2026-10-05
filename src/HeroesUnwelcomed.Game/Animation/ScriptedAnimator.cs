using HeroesUnwelcomed.Game.Sound;

namespace HeroesUnwelcomed.Game.Animation;

/// <summary>
/// Shows one clip that was started on an exact game beat. Used by the
/// choreography, where every step has a fixed start and length.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="AnimationPlayer"/> (which counts time up frame by frame),
/// this keeps no running timer. The frame is calculated from "how many beats
/// since this clip started", so two heroes told to start the same clip on the
/// same beat are on the same frame forever, by construction.
/// </para>
/// <para>
/// <b>Rules:</b> looping music-clock clips (Idle) follow the global beat, like
/// everywhere else. Other looping clips (Walk) cycle from their start beat.
/// One-shots play through once and then hold their last frame until the next
/// step replaces them. The step's length is the rule, not the clip's. While
/// paused, game-clock clips show Idle instead.
/// </para>
/// </remarks>
public sealed class ScriptedAnimator
{
    private ClipDefinition _clip;
    private double _startBeat;

    public ScriptedAnimator(EntityDefinition entity)
    {
        Entity = entity;
        _clip = entity.Idle;
    }

    public EntityDefinition Entity { get; private set; }

    public ClipDefinition Clip => _clip;

    /// <summary>Starts <paramref name="clipName"/> at <paramref name="startBeat"/>. Unknown clips fall back to Idle, so missing art never breaks the flow.</summary>
    public void Play(string clipName, double startBeat)
    {
        _clip = Entity.HasClip(clipName) ? Entity.Clip(clipName) : Entity.Idle;
        _startBeat = startBeat;
    }

    /// <summary>Swaps in reloaded definitions, keeping the current clip if it still exists.</summary>
    public void Rebind(EntityDefinition entity)
    {
        Entity = entity;
        _clip = entity.HasClip(_clip.Name) ? entity.Clip(_clip.Name) : entity.Idle;
    }

    /// <summary>The clip and frame to draw at <paramref name="gameBeat"/>.</summary>
    public (ClipDefinition Clip, int Frame) FrameAt(double gameBeat, BeatTime time)
    {
        ClipDefinition clip = time.Paused && _clip.Clock == AnimationClock.Sim ? Entity.Idle : _clip;

        if (clip.Loop && clip.Clock == AnimationClock.Music)
        {
            return (clip, Wrap((int)Math.Floor(time.MusicBeat / clip.BeatsPerFrame), clip.Frames));
        }

        int frame = (int)Math.Floor(Math.Max(0, gameBeat - _startBeat) / clip.BeatsPerFrame);
        return (clip, clip.Loop ? Wrap(frame, clip.Frames) : Math.Min(frame, clip.Frames - 1));
    }

    /// <summary>A clip shown at a fraction of its length (0..1), e.g. a portal clip tied to distance rather than time.</summary>
    public (ClipDefinition Clip, int Frame) FrameAtProgress(string clipName, float progress)
    {
        ClipDefinition clip = Entity.HasClip(clipName) ? Entity.Clip(clipName) : _clip;
        int frame = (int)Math.Floor(Math.Clamp(progress, 0f, 0.999f) * clip.Frames);
        return (clip, frame);
    }

    private static int Wrap(int value, int count) => ((value % count) + count) % count;
}
