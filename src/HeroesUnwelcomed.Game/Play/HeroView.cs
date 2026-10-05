using HeroesUnwelcomed.Game.Animation;
using HeroesUnwelcomed.Game.Sound;
using Microsoft.Xna.Framework;

namespace HeroesUnwelcomed.Game.Play;

/// <summary>
/// How one hero looks on screen: where they stand, which animation is
/// playing, and whether they're fading out after death.
/// </summary>
/// <remarks>
/// <para>
/// The view never decides anything. The simulation says where the hero
/// *should* be (a queue slot, the fight spot, or a route to the next room)
/// and this class moves the picture there smoothly.
/// </para>
/// <para>
/// <b>Moving between rooms</b> follows a <see cref="MovementPath"/> timed by the
/// game beat, so travel takes exactly as long as the simulation says.
/// <b>Within a room</b> (e.g. after the huddle reorders the queue) the hero
/// glides toward the target. The glide also runs on game beats, so it freezes
/// while paused and speeds up at 3x like everything else.
/// </para>
/// <para>
/// <b>Animations:</b> event-driven one-shots (Attack, Fear, Huddle,
/// Celebrate, Death) play to completion. Otherwise the hero walks while moving
/// and idles while still.
/// </para>
/// </remarks>
public sealed class HeroView
{
    private const string Idle = "Idle";
    private const string Walk = "Walk";
    private const string Death = "Death";
    private const double FadeAfterBeats = 3;
    private const double FadeOverBeats = 2;

    private MovementPath? _path;
    private double _pathStart;
    private double _pathEnd;
    private double? _diedAtBeat;

    public HeroView(int heroId, string name, EntityDefinition entity)
    {
        HeroId = heroId;
        Name = name;
        Animation = new AnimationPlayer(entity);
    }

    public int HeroId { get; }
    public string Name { get; }
    public AnimationPlayer Animation { get; }
    public Vector2 Position { get; private set; }
    public Vector2 Target { get; set; }
    public bool Visible { get; private set; }
    public float Opacity { get; private set; } = 1f;
    public bool IsMoving { get; private set; }
    public bool IsDead => _diedAtBeat is not null;

    /// <summary>Puts the hero in the world (a new party just entered).</summary>
    public void Appear(Vector2 position)
    {
        Position = Target = position;
        Visible = true;
        Opacity = 1f;
        _diedAtBeat = null;
        _path = null;
        Animation.Play(Idle);
    }

    public void FollowPath(MovementPath path, double startBeat, double endBeat)
    {
        _path = path;
        _pathStart = startBeat;
        _pathEnd = endBeat;
        Target = path.End;
    }

    /// <summary>Plays an action animation (if the hero has it), unless they're dead.</summary>
    public void PlayOneShot(string clip)
    {
        if (!IsDead && Animation.Entity.HasClip(clip))
        {
            Animation.Play(clip);
        }
    }

    public void Die(double beat)
    {
        _diedAtBeat = beat;
        _path = null;
        Target = Position;
        Animation.Play(Death);
    }

    /// <param name="gameBeat">Continuous game beat since the run started (smoother than the simulation's 1/8-beat ticks).</param>
    public void Update(BeatTime time, double gameBeat)
    {
        if (!Visible)
        {
            return;
        }

        if (_path is not null)
        {
            float t = (float)((gameBeat - _pathStart) / Math.Max(1e-6, _pathEnd - _pathStart));
            Position = _path.At(t);
            if (t >= 1f)
            {
                _path = null;
            }
        }
        else
        {
            // Glide toward the target: cover ~90% of the gap per game beat.
            float amount = 1f - MathF.Pow(0.1f, (float)time.SimDelta);
            Position = Vector2.Distance(Position, Target) > 2500
                ? Target // too far to glide (e.g. a scene jump): snap
                : Vector2.Lerp(Position, Target, amount);
        }

        IsMoving = _path is not null || Vector2.Distance(Position, Target) > 6f;
        ChooseAnimation();
        Animation.Update(time);

        if (_diedAtBeat is { } died)
        {
            double fade = (gameBeat - died - FadeAfterBeats) / FadeOverBeats;
            Opacity = (float)Math.Clamp(1 - fade, 0, 1);
            if (Opacity <= 0)
            {
                Visible = false;
            }
        }
    }

    private void ChooseAnimation()
    {
        if (IsDead)
        {
            return; // Death holds its last frame
        }

        ClipDefinition current = Animation.Clip;
        bool inOneShot = !current.Loop;
        if (inOneShot)
        {
            return; // let it finish; its "then" brings it back to Idle
        }

        string wanted = IsMoving ? Walk : Idle;
        if (current.Name != wanted)
        {
            Animation.Play(wanted);
        }
    }
}
