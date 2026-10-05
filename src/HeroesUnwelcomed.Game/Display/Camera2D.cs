using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game.Display;

/// <summary>
/// The view onto the world: where it's looking and how far it's zoomed.
/// </summary>
/// <remarks>
/// <para>
/// <b>The matrix.</b> To draw the world through the camera, every world point
/// goes through three steps, multiplied together into one matrix (as in your
/// original camera):
/// <list type="number">
///   <item>Translate by −<see cref="Position"/>: the point the camera looks at becomes (0,0).</item>
///   <item>Scale by <see cref="Zoom"/>: zoom in or out around that point.</item>
///   <item>Translate by half the screen size: (0,0) moves to the middle of the screen.</item>
/// </list>
/// Going the other way (mouse → world) uses the inverse matrix.
/// </para>
/// <para>
/// <b>Zooming around a point.</b> To zoom "toward the mouse", remember which
/// world point is under the cursor, change the zoom, then move the camera so
/// that same world point is back under the cursor.
/// </para>
/// </remarks>
public sealed class Camera2D
{
    public Vector2 Position { get; set; }
    public float Zoom { get; private set; } = 0.5f;
    public float MinZoom { get; set; } = 0.1f;
    public float MaxZoom { get; set; } = 2f;

    /// <summary>The area the camera may show, in world units. The centre is kept inside it.</summary>
    public Rectangle Bounds { get; set; }

    public Matrix Transform(Viewport viewport) =>
        Matrix.CreateTranslation(-Position.X, -Position.Y, 0f)
        * Matrix.CreateScale(Zoom, Zoom, 1f)
        * Matrix.CreateTranslation(viewport.Width / 2f, viewport.Height / 2f, 0f);

    public Vector2 ScreenToWorld(Vector2 screen, Viewport viewport) =>
        Vector2.Transform(screen, Matrix.Invert(Transform(viewport)));

    public Vector2 WorldToScreen(Vector2 world, Viewport viewport) =>
        Vector2.Transform(world, Transform(viewport));

    /// <summary>The world rectangle currently on screen.</summary>
    public Rectangle VisibleWorld(Viewport viewport)
    {
        Vector2 size = new Vector2(viewport.Width, viewport.Height) / Zoom;
        return new Rectangle((int)(Position.X - size.X / 2), (int)(Position.Y - size.Y / 2), (int)size.X, (int)size.Y);
    }

    /// <summary>Moves the camera by a distance measured in screen pixels (so panning feels the same at any zoom).</summary>
    public void PanByScreen(Vector2 screenDelta)
    {
        Position += screenDelta / Zoom;
        Clamp();
    }

    /// <summary>Multiplies the zoom, keeping the world point under <paramref name="screenAnchor"/> fixed on screen.</summary>
    public void ZoomAt(float factor, Vector2 screenAnchor, Viewport viewport)
    {
        Vector2 before = ScreenToWorld(screenAnchor, viewport);
        Zoom = Math.Clamp(Zoom * factor, MinZoom, MaxZoom);
        Vector2 after = ScreenToWorld(screenAnchor, viewport);
        Position += before - after;
        Clamp();
    }

    /// <summary>Centres on <paramref name="area"/> and zooms so all of it fits, with a margin.</summary>
    public void FitTo(Rectangle area, Viewport viewport, float margin = 0.9f)
    {
        Position = area.Center.ToVector2();
        float fit = Math.Min(viewport.Width / (float)area.Width, viewport.Height / (float)area.Height) * margin;
        Zoom = Math.Clamp(fit, MinZoom, MaxZoom);
        Clamp();
    }

    /// <summary>Glides toward showing <paramref name="target"/> (used to follow a controller's selection).</summary>
    public void EnsureVisible(Rectangle target, Viewport viewport, float smoothing)
    {
        Rectangle visible = VisibleWorld(viewport);
        Vector2 shift = Vector2.Zero;
        if (target.Left < visible.Left) shift.X = target.Left - visible.Left;
        else if (target.Right > visible.Right) shift.X = target.Right - visible.Right;
        if (target.Top < visible.Top) shift.Y = target.Top - visible.Top;
        else if (target.Bottom > visible.Bottom) shift.Y = target.Bottom - visible.Bottom;

        Position += shift * Math.Clamp(smoothing, 0f, 1f);
        Clamp();
    }

    private void Clamp()
    {
        if (Bounds.IsEmpty) return;
        Position = new Vector2(
            Math.Clamp(Position.X, Bounds.Left, Bounds.Right),
            Math.Clamp(Position.Y, Bounds.Top, Bounds.Bottom));
    }
}
