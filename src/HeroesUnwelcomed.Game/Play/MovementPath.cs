using Microsoft.Xna.Framework;

namespace HeroesUnwelcomed.Game.Play;

/// <summary>
/// A route through the world made of straight segments, some of which are
/// instant jumps (portals). Positions are looked up by distance walked.
/// </summary>
/// <remarks>
/// <para>
/// <b>Distance, not time.</b> A whole party shares one route. Each hero is
/// simply a fixed distance further back along it, and everyone advances by
/// the same amount, so the line keeps its spacing exactly, even through doors
/// and portals.
/// </para>
/// <para>
/// Portal jumps have no length: reaching the entry point means you're already
/// at the exit point. <see cref="JumpDistances"/> says where along the route
/// they happen, so heroes near one can play Portal Entrance / Portal Exit.
/// </para>
/// </remarks>
public sealed class MovementPath
{
    private readonly List<(Vector2 Point, bool JumpTo)> _points = [];
    private readonly List<float> _jumpDistances = [];
    private float _length;

    public MovementPath(Vector2 start)
    {
        _points.Add((start, false));
    }

    public Vector2 End => _points[^1].Point;

    /// <summary>Total walked distance (jumps count as zero).</summary>
    public float WalkedLength => _length;

    /// <summary>Distances along the route where a portal jump happens.</summary>
    public IReadOnlyList<float> JumpDistances => _jumpDistances;

    /// <summary>Walk in a straight line to <paramref name="point"/>.</summary>
    public MovementPath WalkTo(Vector2 point)
    {
        _length += Vector2.Distance(_points[^1].Point, point);
        _points.Add((point, false));
        return this;
    }

    /// <summary>Appear instantly at <paramref name="point"/> (step through a portal).</summary>
    public MovementPath JumpTo(Vector2 point)
    {
        _jumpDistances.Add(_length);
        _points.Add((point, true));
        return this;
    }

    /// <summary>Position at <paramref name="t"/> (0 = start, 1 = end) of the walked distance.</summary>
    public Vector2 At(float t) => AtDistance(Math.Clamp(t, 0f, 1f) * _length);

    /// <summary>Position <paramref name="distance"/> units along the route (clamped to the ends).</summary>
    public Vector2 AtDistance(float distance)
    {
        float remaining = Math.Clamp(distance, 0f, _length);

        for (int i = 1; i < _points.Count; i++)
        {
            var (from, _) = _points[i - 1];
            var (to, jump) = _points[i];
            if (jump)
            {
                continue; // no distance: arriving at the previous point means you're already through
            }

            float length = Vector2.Distance(from, to);
            if (remaining < length || (i == _points.Count - 1 && remaining <= length))
            {
                return length > 0 ? Vector2.Lerp(from, to, remaining / length) : to;
            }
            remaining -= length;
        }
        return End;
    }
}
