using HeroesUnwelcomed.Game.Animation;
using HeroesUnwelcomed.Game.Sound;
using Microsoft.Xna.Framework;

namespace HeroesUnwelcomed.Game.Play;

/// <summary>
/// One hero on screen. It holds no logic of its own: its party tells it, for
/// each choreography step, where to move (a stretch of a route, start beat,
/// end beat) and which clip to play (from a start beat).
/// </summary>
/// <remarks>
/// Position is calculated, never accumulated: "how far along my route should I
/// be at this beat". So heroes given the same timing move in perfect lockstep,
/// and pausing (game beat stops) freezes them exactly in place.
/// </remarks>
public sealed class HeroView
{
    private const float PortalClipDistance = 160f; // world units either side of a portal jump

    private MovementPath? _path;
    private float _fromDistance;
    private float _toDistance;
    private double _moveStart;
    private double _moveEnd;
    private Vector2 _standing;

    public HeroView(int heroId, string name, EntityDefinition entity)
    {
        HeroId = heroId;
        Name = name;
        Animator = new ScriptedAnimator(entity);
    }

    public int HeroId { get; }
    public string Name { get; }
    public ScriptedAnimator Animator { get; }
    public bool Visible { get; set; }

    /// <summary>Where the hero stands once any current movement finishes.</summary>
    public Vector2 RestingPosition => _path is null ? _standing : _path.AtDistance(_toDistance);

    /// <summary>Stand still at <paramref name="position"/> (cancels any movement).</summary>
    public void PlaceAt(Vector2 position)
    {
        _path = null;
        _standing = position;
    }

    /// <summary>Move along <paramref name="path"/> from one distance to another between two beats.</summary>
    public void Move(MovementPath path, float fromDistance, float toDistance, double startBeat, double endBeat)
    {
        _path = path;
        _fromDistance = fromDistance;
        _toDistance = toDistance;
        _moveStart = startBeat;
        _moveEnd = endBeat;
    }

    /// <summary>Walk in a straight line from where the hero stands to <paramref name="to"/>.</summary>
    public void MoveTo(Vector2 to, double startBeat, double endBeat)
    {
        var path = new MovementPath(RestingPosition).WalkTo(to);
        Move(path, 0, path.WalkedLength, startBeat, endBeat);
    }

    public Vector2 PositionAt(double gameBeat)
    {
        if (_path is null) return _standing;
        return _path.AtDistance(DistanceAt(gameBeat));
    }

    /// <summary>What to draw now. Near a portal jump, Portal Entrance / Portal Exit replace Walk.</summary>
    public (ClipDefinition Clip, int Frame) FrameAt(double gameBeat, BeatTime time)
    {
        if (_path is not null && !time.Paused && gameBeat < _moveEnd)
        {
            float distance = DistanceAt(gameBeat);
            foreach (float jump in _path.JumpDistances)
            {
                float before = jump - distance;
                if (before > 0 && before < PortalClipDistance)
                {
                    return Animator.FrameAtProgress("PortalEntrance", 1f - before / PortalClipDistance);
                }
                float after = distance - jump;
                if (after >= 0 && after < PortalClipDistance)
                {
                    return Animator.FrameAtProgress("PortalExit", after / PortalClipDistance);
                }
            }
        }
        return Animator.FrameAt(gameBeat, time);
    }

    private float DistanceAt(double gameBeat)
    {
        double t = Math.Clamp((gameBeat - _moveStart) / Math.Max(1e-6, _moveEnd - _moveStart), 0, 1);
        return MathHelper.Lerp(_fromDistance, _toDistance, (float)t);
    }
}
