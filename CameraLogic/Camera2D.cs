using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;

public sealed class Camera2D
{
    public Vector2 Position = Vector2.Zero;
    public float Zoom = 1f;
    public float Rotation = 0f;

    public float MinZoom = 0.25f;   // will be recomputed
    public float MaxZoom = 4f;

    private Rectangle _world = new Rectangle(0, 0, 0, 0);

    public void SetWorld(Rectangle world, GraphicsDevice gd)
    {
        _world = world;
        RecomputeMinZoom(gd);     // fit-all zoom based on world & viewport
        Zoom = MathHelper.Clamp(Zoom, MinZoom, MaxZoom);
        ClampPositionToWorld(gd);
    }

    public void RecomputeMinZoom(GraphicsDevice gd)
    {
        var vp = gd.Viewport;
        if (_world.Width <= 0 || _world.Height <= 0 || vp.Width <= 0 || vp.Height <= 0)
        {
            MinZoom = 0.25f; // fallback
            return;
        }

        // Smallest zoom that still shows the entire world (no cropping):
        float zx = (float)vp.Width / _world.Width;
        float zy = (float)vp.Height / _world.Height;
        MinZoom = Math.Min(zx, zy);

        // Keep a sane MaxZoom too (optional)
        MaxZoom = Math.Max(MaxZoom, MinZoom * 8f); // e.g., allow up to 8× closer than fit-all
    }

    public void ClampPositionToWorld(GraphicsDevice gd)
    {
        // Assumes Rotation == 0; rotation-aware clamping is more complex.
        var vp = gd.Viewport;

        // Half of the visible size in world units at current zoom
        float halfViewW = (vp.Width * 0.5f) / Zoom;
        float halfViewH = (vp.Height * 0.5f) / Zoom;

        // If world is smaller than view in a dimension, just center
        float minX = _world.Left + halfViewW;
        float maxX = _world.Right - halfViewW;
        float minY = _world.Top + halfViewH;
        float maxY = _world.Bottom - halfViewH;

        // If min > max, the screen is larger than the world in that axis
        float centerX = _world.Left + _world.Width * 0.5f;
        float centerY = _world.Top + _world.Height * 0.5f;

        float clampedX = (minX <= maxX) ? MathHelper.Clamp(Position.X, minX, maxX) : centerX;
        float clampedY = (minY <= maxY) ? MathHelper.Clamp(Position.Y, minY, maxY) : centerY;

        Position = new Vector2(clampedX, clampedY);
    }

    public Matrix GetViewMatrix(GraphicsDevice graphicsDevice)
    {
        float zoom = Zoom;
        Vector2 pos = Position;

        // Snap the camera position so that (pos * zoom) lands on whole pixels
        var snappedPos = new Vector2(
            (float)Math.Round(pos.X * zoom) / zoom,
            (float)Math.Round(pos.Y * zoom) / zoom
        );

        return
            Matrix.CreateTranslation(new Vector3(-snappedPos, 0f)) *
            Matrix.CreateScale(zoom, zoom, 1f) *
            Matrix.CreateTranslation(
                graphicsDevice.Viewport.Width * 0.5f,
                graphicsDevice.Viewport.Height * 0.5f,
                0f
            );
    }

    public void ClampZoom()
    {
        Zoom = MathHelper.Clamp(Zoom, MinZoom, MaxZoom);
    }
    public Vector2 ScreenToWorld(Vector2 screen, GraphicsDevice gd)
        => Vector2.Transform(screen, Matrix.Invert(GetViewMatrix(gd)));
}
