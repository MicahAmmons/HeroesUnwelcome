using Heroes_UnWelcomed.Cells;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using Heroes_UnWelcomed.InputTracker;

public static class CameraManager
{
    private static GraphicsDevice _graphics;
    private static readonly Camera2D _camera = new Camera2D
    {
        MinZoom = 0.125f,
        MaxZoom = 8f,
        Zoom = 1f
    };
    public static Camera2D Camera => _camera;

    private static readonly SmoothZoomInputController _zoom = new SmoothZoomInputController();

    private static bool _worldAppliedOnce;
    private static Rectangle? _deferredWorldRect;

    public static void Intitialize()
    {
        CellManager.TheWorldRectChanged += rect => OnWorldRectChanged(rect);

    }
    // CameraManager.cs (new helper)
    public static void FitWorldToViewport(Rectangle world)
    {
        if (_graphics == null) return;
        var vp = _graphics.Viewport;

        // Compute zoom that fits world entirely in viewport
        float zoomX = vp.Width / (float)world.Width;
        float zoomY = vp.Height / (float)world.Height;
        float targetZoom = Math.Min(zoomX, zoomY);

        _camera.Zoom = MathHelper.Clamp(targetZoom, _camera.MinZoom, _camera.MaxZoom);

        // Center camera on the world's center
        _camera.Position = new Vector2(world.Center.X, world.Center.Y);

        _camera.ClampPositionToWorld(_graphics);
    }
    public static Vector2 ScreenToWorld(Vector2 screenPos)
    {
        if (_graphics == null)
            throw new InvalidOperationException("CameraManager.ScreenToWorld called before _graphics was set.");

        return _camera.ScreenToWorld(screenPos, _graphics);
    }
    public static Vector2 GetMouseWorldPosition(Point mouse)
    {
        if (_graphics == null)
            return Vector2.One;

        return _camera.ScreenToWorld(mouse.ToVector2(), _graphics);
    }
    internal static void Update(GraphicsDevice graphicsDevice)
    {
        _graphics = graphicsDevice;

        // Apply any deferred world rect (e.g., event fired before _graphics set)
        if (_deferredWorldRect.HasValue)
        {
            ApplyWorld(_deferredWorldRect.Value);
            _deferredWorldRect = null;
        }

        _zoom.Update(_camera, _graphics);

        // If you support window resize, call this from Game.Window.ClientSizeChanged:
        // OnViewportChanged();
    }

    private static void OnWorldRectChanged(Rectangle rect)
    {
        if (_graphics == null)
        {
            _deferredWorldRect = rect; // apply next Update
            return;
        }
        ApplyWorld(rect);
    }

    private static void ApplyWorld(Rectangle rect)
    {
        _camera.SetWorld(rect, _graphics);

        if (!_worldAppliedOnce)
        {
            _worldAppliedOnce = true;
            _camera.Position = new Vector2(rect.Center.X, rect.Center.Y);
            _camera.ClampPositionToWorld(_graphics);
        }
    }

    // Call this from Game.Window.ClientSizeChanged (or after toggling fullscreen)
    public static void OnViewportChanged()
    {
        if (_graphics == null) return;
        _camera.RecomputeMinZoom(_graphics);
        _camera.Zoom = MathHelper.Clamp(_camera.Zoom, _camera.MinZoom, _camera.MaxZoom);
        _camera.ClampPositionToWorld(_graphics);
    }

}
