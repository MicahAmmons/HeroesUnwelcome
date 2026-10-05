using Microsoft.Xna.Framework;

namespace HeroesUnwelcomed.Game.Play;

/// <summary>
/// A route through the world made of straight segments, some of which are
/// instant jumps (portals). <see cref="At"/> gives the position a fraction of
/// the way along it.
/// </summary>
/// <remarks>
/// Progress is measured by distance walked, not by point count, so walking
/// speed is even along the whole route. Portal jumps have no length: the hero
/// arrives at the portal, then appears at the other one on the very next instant.
/// </remarks>
public sealed class MovementPath
{
    private readonly List<(Vector2 Point, bool JumpTo)> _points = [];

    public MovementPath(Vector2 start)
    {
        _points.Add((start, false));
    }

    public Vector2 End => _points[^1].Point;

    /// <summary>Walk in a straight line to <paramref name="point"/>.</summary>
    public MovementPath WalkTo(Vector2 point)
    {
        _points.Add((point, false));
        return this;
    }

    /// <summary>Appear instantly at <paramref name="point"/> (step through a portal).</summary>
    public MovementPath JumpTo(Vector2 point)
    {
        _points.Add((point, true));
        return this;
    }

    public float WalkedLength
    {
        get
        {
            float total = 0;
            for (int i = 1; i < _points.Count; i++)
            {
                if (!_points[i].JumpTo) total += Vector2.Distance(_points[i - 1].Point, _points[i].Point);
            }
            return total;
        }
    }

    /// <summary>Position at <paramref name="t"/> (0 = start, 1 = end) of the walked distance.</summary>
    public Vector2 At(float t)
    {
        float remaining = Math.Clamp(t, 0f, 1f) * WalkedLength;

        for (int i = 1; i < _points.Count; i++)
        {
            var (from, _) = _points[i - 1];
            var (to, jump) = _points[i];
            if (jump)
            {
                continue; // no distance: arriving at the previous point means you're already through
            }

            float length = Vector2.Distance(from, to);
            if (remaining <= length && length > 0)
            {
                return Vector2.Lerp(from, to, remaining / length);
            }
            remaining -= length;
        }
        return End;
    }
}
