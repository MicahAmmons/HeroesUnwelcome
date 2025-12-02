using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;

public sealed class SmoothZoomInputController
{
    // ---- Tunables ----
    public float ZoomAcceleration = 0.026f;
    public float ZoomDampening = 0.40f;
    public float ZoomLerpFactor = 0.10f;
    public bool CtrlBoost = true;
    public float CtrlBoostMul = 2.0f;

    // Pan
    public bool AllowMiddleMousePan = true; // MMB also pans
    public bool AllowSpacebarPan = true; // Space + LMB pans (Photoshop-style)

    // Trackpad behavior
    public bool EnableTrackpadPan = true; // two-finger slide pans
    public float PanPixelsPerNotch = 60f;  // pixels per 120 wheel units (screen-space)

    // Zoom rules (your spec)
    public bool RequireCtrlForWheelZoom = true; // Ctrl + wheel (incl. trackpad scroll) = zoom
                                                // Wheel without Ctrl is NOT zoom (it pans instead)

    // Pinch behavior
    public bool EnablePinchZoom = false;                // per spec: pinch should NOT zoom
    public bool FallbackTwoFingerPanFromPinch = true;   // if device emits pinch instead of wheel for slides
    public float PinchScaleDeadzone = 0.02f;            // <=2% scale change treated as "no zoom" (used for pan)

    // --------------- state ---------------
    private int _prevWheel;
    private int _prevHWheel;
    private float _zoomVelocity = 0f;
    private float _targetZoom = 1f;
    private bool _initialized = false;
    private bool _pinchInitTried = false;

    // Panning state
    private Point _lastMousePos;
    private bool _dragging = false;

    public SmoothZoomInputController() { }

    public void Update(Camera2D cam, GraphicsDevice gd)
    {
        if (!_initialized)
        {
            _initialized = true;
            _targetZoom = cam.Zoom;
            var ms = Mouse.GetState();
            _prevWheel = ms.ScrollWheelValue;
            _prevHWheel = GetHorizontalWheel(ms);
            _lastMousePos = ms.Position;

            EnsurePinchEnabledOnceSafe();
        }

        var msNow = Mouse.GetState();

        // --- 1) Two-finger gestures via TouchPanel: treat as PAN when scale ~ 1 ---
        bool consumedPinch = false;
        if (FallbackTwoFingerPanFromPinch)
        {
            TryPanFromPinchMidpoint(cam, gd, ref consumedPinch);
        }

        // --- 2) Trackpad wheel -> PAN (when no Ctrl). This consumes wheel deltas. ---
        bool pannedViaTrackpad = HandleTrackpadPan(cam, gd, msNow);

        // --- 3) Accumulate ZOOM only from wheel when Ctrl is held (and optionally real pinch zoom if enabled) ---
        if (!pannedViaTrackpad && !consumedPinch)
        {
            AccumulateMouseWheelForZoom(ref _zoomVelocity); // respects RequireCtrlForWheelZoom
            if (EnablePinchZoom)
                AccumulatePinchForZoom(ref _zoomVelocity);
        }

        _targetZoom *= 1f + _zoomVelocity;
        _targetZoom = MathHelper.Clamp(_targetZoom, cam.MinZoom, cam.MaxZoom);

        var mouseScreen = msNow.Position.ToVector2();
        ApplyZoomWithAnchor(cam, gd, mouseScreen, Smooth(cam.Zoom, _targetZoom, ZoomLerpFactor));

        // --- 4) Classic panning (RMB/MMB/Space+LMB) ---
        HandleButtonPanning(cam, gd, msNow);

        // --- 5) Decay zoom velocity & update last mouse ---
        _zoomVelocity *= (1f - MathHelper.Clamp(ZoomDampening, 0f, 1f));
        _lastMousePos = msNow.Position;
    }

    // -------------------- PAN: mouse buttons --------------------
    private void HandleButtonPanning(Camera2D cam, GraphicsDevice gd, MouseState msNow)
    {
        bool rightDown = msNow.RightButton == ButtonState.Pressed;
        bool middleDown = AllowMiddleMousePan && (msNow.MiddleButton == ButtonState.Pressed);

        bool spacePan = false;
        if (AllowSpacebarPan)
        {
            var ks = Keyboard.GetState();
            spacePan = ks.IsKeyDown(Keys.Space) && msNow.LeftButton == ButtonState.Pressed;
        }

        bool shouldDrag = rightDown || middleDown || spacePan;

        if (shouldDrag && !_dragging) _dragging = true;
        if (!shouldDrag && _dragging) _dragging = false;
        if (!_dragging) return;

        var deltaScreen = (msNow.Position - _lastMousePos).ToVector2();
        if (deltaScreen == Vector2.Zero) return;

        var deltaWorld = -deltaScreen / cam.Zoom;
        cam.Position += deltaWorld;
        cam.ClampPositionToWorld(gd);
    }

    // -------------------- PAN: trackpad wheel (two-finger slide) --------------------
    private bool HandleTrackpadPan(Camera2D cam, GraphicsDevice gd, MouseState msNow)
    {
        if (!EnableTrackpadPan) return false;

        // If a button-based pan is happening, don't treat wheel as pan.
        bool anyButtonDown = msNow.LeftButton == ButtonState.Pressed
                          || msNow.RightButton == ButtonState.Pressed
                          || msNow.MiddleButton == ButtonState.Pressed;

        bool spacePanActive = false;
        if (AllowSpacebarPan)
        {
            var ks = Keyboard.GetState();
            spacePanActive = ks.IsKeyDown(Keys.Space) && msNow.LeftButton == ButtonState.Pressed;
        }
        if (anyButtonDown || spacePanActive) return false;

        // If Ctrl is held, wheel should be reserved for ZOOM, not pan.
        var k = Keyboard.GetState();
        bool ctrl = k.IsKeyDown(Keys.LeftControl) || k.IsKeyDown(Keys.RightControl);
        if (RequireCtrlForWheelZoom && ctrl) return false;

        int vDelta = msNow.ScrollWheelValue - _prevWheel;
        int hNow = GetHorizontalWheel(msNow);
        int hDelta = hNow - _prevHWheel;

        _prevWheel = msNow.ScrollWheelValue;
        _prevHWheel = hNow;

        if (vDelta == 0 && hDelta == 0) return false;

        float vx = -(hDelta / 120f) * PanPixelsPerNotch; // +right/-left
        float vy = (vDelta / 120f) * PanPixelsPerNotch; // +down/-up  (invert here if you prefer)

        var deltaScreen = new Vector2(vx, vy);
        var deltaWorld = -deltaScreen / cam.Zoom;
        cam.Position += deltaWorld;
        cam.ClampPositionToWorld(gd);

        return true;
    }

    // -------------------- PAN fallback: pinch midpoint when scale ~ 1 --------------------
    private void TryPanFromPinchMidpoint(Camera2D cam, GraphicsDevice gd, ref bool consumedPinch)
    {
        // Enable pinch recognition once
        EnsurePinchEnabledOnceSafe();

        while (TouchPanel.IsGestureAvailable)
        {
            var g = TouchPanel.ReadGesture();
            if (g.GestureType != GestureType.Pinch) continue;

            consumedPinch = true; // we’ll handle it (either as pan or (optionally) zoom)

            var prev1 = g.Position - g.Delta;
            var prev2 = g.Position2 - g.Delta2;
            float prevDist = Vector2.Distance(prev1, prev2);
            if (prevDist <= 0.0001f) continue;

            float currDist = Vector2.Distance(g.Position, g.Position2);
            float scale = currDist / prevDist;

            // Midpoint movement = "two-finger slide"
            var prevMid = (prev1 + prev2) * 0.5f;
            var currMid = (g.Position + g.Position2) * 0.5f;
            var midDeltaScreen = currMid - prevMid;

            // Treat as PAN if scale change is tiny, or pinch-zoom is disabled
            if (!EnablePinchZoom || Math.Abs(scale - 1f) < PinchScaleDeadzone)
            {
                var deltaWorld = -midDeltaScreen / cam.Zoom;
                cam.Position += deltaWorld;
                cam.ClampPositionToWorld(gd);
            }
            else
            {
                // If you ever enable pinch zoom, you can feed it here:
                // _zoomVelocity += (scale - 1f);
            }
        }
    }

    // -------------------- ZOOM: mouse wheel (Ctrl required per spec) --------------------
    private void AccumulateMouseWheelForZoom(ref float velocity)
    {
        var ms = Mouse.GetState();

        int delta = ms.ScrollWheelValue - _prevWheel;
        _prevWheel = ms.ScrollWheelValue;
        if (delta == 0) return;

        var ks = Keyboard.GetState();
        bool ctrl = ks.IsKeyDown(Keys.LeftControl) || ks.IsKeyDown(Keys.RightControl);

        // Only zoom if Ctrl is held (covers: mouse wheel; trackpad scroll while holding Ctrl)
        if (RequireCtrlForWheelZoom && !ctrl) return;

        float notches = delta / 120f;
        float boost = (CtrlBoost && ctrl) ? CtrlBoostMul : 1f;
        velocity += notches * ZoomAcceleration * boost;
    }

    // -------------------- ZOOM: pinch (optional) --------------------
    private void AccumulatePinchForZoom(ref float velocity)
    {
        // Only used if EnablePinchZoom == true
        while (TouchPanel.IsGestureAvailable)
        {
            var g = TouchPanel.ReadGesture();
            if (g.GestureType != GestureType.Pinch) continue;

            var prev1 = g.Position - g.Delta;
            var prev2 = g.Position2 - g.Delta2;
            float prevDist = Vector2.Distance(prev1, prev2);
            if (prevDist <= 0.0001f) continue;

            float currDist = Vector2.Distance(g.Position, g.Position2);
            float factor = currDist / prevDist; // >1 in, <1 out
            velocity += (factor - 1f);
        }
    }

    // -------------------- Helpers --------------------
    private void EnsurePinchEnabledOnceSafe()
    {
        if (_pinchInitTried) return;
        _pinchInitTried = true;
        try
        {
            var caps = TouchPanel.GetCapabilities();
            if (caps.IsConnected)
                TouchPanel.EnabledGestures |= GestureType.Pinch;
        }
        catch { /* ignore */ }
    }

    private static float Smooth(float current, float target, float t)
        => current + (target - current) * MathHelper.Clamp(t, 0f, 1f);

    private static void ApplyZoomWithAnchor(Camera2D cam, GraphicsDevice gd, Vector2 screenAnchor, float newZoom)
    {
        newZoom = MathHelper.Clamp(newZoom, cam.MinZoom, cam.MaxZoom);
        if (Math.Abs(newZoom - cam.Zoom) < 1e-6f) return;

        var before = cam.ScreenToWorld(screenAnchor, gd);
        cam.Zoom = newZoom;
        cam.ClampZoom();
        var after = cam.ScreenToWorld(screenAnchor, gd);
        cam.Position += (before - after);
        cam.ClampPositionToWorld(gd);
    }

    private static int GetHorizontalWheel(MouseState ms)

    {
        // DesktopGL exposes HorizontalScrollWheelValue; WindowsDX may not.
        // If your target doesn't have it, change to: return 0;
        return ms.HorizontalScrollWheelValue;
    }

}
