using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace HeroesUnwelcomed.Game.Input;

/// <summary>Which kind of device the player touched most recently.</summary>
public enum InputDevice
{
    KeyboardMouse,
    Gamepad,
}

/// <summary>
/// Reads every device once per frame and answers questions in terms of
/// <see cref="GameAction"/>s. The rest of the game never touches
/// Keyboard/Mouse/GamePad directly.
/// </summary>
/// <remarks>
/// <para>
/// <b>Down / Pressed / Released.</b> We keep last frame's state next to this
/// frame's. "Down" means held right now. "Pressed" means down now but not last
/// frame (true for exactly one frame). "Released" is the reverse. Your old TapTap
/// class did the same thing for raw keys; this does it per action.
/// </para>
/// <para>
/// <b>Active device.</b> Whichever device was used last becomes active. UI uses
/// this to decide whether to show the mouse cursor or a controller-style
/// highlight, and which button prompts to display.
/// </para>
/// <para>
/// <b>Continuous input.</b> Besides on/off actions, the camera needs analog
/// values: <see cref="Pan"/> (right stick or WASD) and <see cref="Zoom"/>
/// (triggers or mouse wheel). The mouse pointer is exposed separately because
/// pointing is its own kind of input (touch will feed the same properties later).
/// </para>
/// </remarks>
public sealed class InputService
{
    private const float StickDeadzone = 0.2f;
    private const float WheelNotch = 120f; // one click of a mouse wheel

    private readonly InputBindings _bindings;
    private readonly HashSet<GameAction> _down = [];
    private readonly HashSet<GameAction> _previousDown = [];

    private KeyboardState _keyboard;
    private MouseState _mouse;
    private MouseState _previousMouse;
    private GamePadState _gamepad;
    private bool _hasPreviousMouse;

    public InputService(InputBindings bindings)
    {
        _bindings = bindings;
    }

    public InputDevice ActiveDevice { get; private set; } = InputDevice.KeyboardMouse;

    /// <summary>Camera pan direction, each axis -1..1 (right stick or WASD).</summary>
    public Vector2 Pan { get; private set; }

    /// <summary>Zoom amount this frame: positive = in. Triggers give a steady rate, the wheel gives steps.</summary>
    public float Zoom { get; private set; }

    /// <summary>Mouse position in window pixels.</summary>
    public Point PointerPosition => _mouse.Position;

    /// <summary>How far the mouse moved since last frame, in window pixels.</summary>
    public Point PointerDelta { get; private set; }

    public bool PointerPrimaryPressed => _mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
    public bool PointerPrimaryDown => _mouse.LeftButton == ButtonState.Pressed;

    /// <summary>True while right or middle mouse is held: drag-to-pan.</summary>
    public bool PointerDragPanDown => _mouse.RightButton == ButtonState.Pressed || _mouse.MiddleButton == ButtonState.Pressed;

    public bool IsDown(GameAction action) => _down.Contains(action);
    public bool WasPressed(GameAction action) => _down.Contains(action) && !_previousDown.Contains(action);
    public bool WasReleased(GameAction action) => !_down.Contains(action) && _previousDown.Contains(action);

    /// <summary>Reads all devices. Call exactly once per frame, before anything asks about input.</summary>
    /// <param name="windowFocused">When the window isn't focused, all input is ignored.</param>
    /// <param name="keyboardBlocked">True when something else (the debug panel) is using the keyboard.</param>
    /// <param name="mouseBlocked">True when something else (the debug panel) is using the mouse.</param>
    public void Update(float deltaSeconds, bool windowFocused, bool keyboardBlocked = false, bool mouseBlocked = false)
    {
        _previousDown.Clear();
        _previousDown.UnionWith(_down);
        _down.Clear();

        _previousMouse = _mouse;
        _keyboard = windowFocused && !keyboardBlocked ? Keyboard.GetState() : default;
        _mouse = windowFocused && !mouseBlocked ? Mouse.GetState() : KeepPositionOnly(Mouse.GetState());
        _gamepad = windowFocused ? GamePad.GetState(PlayerIndex.One, GamePadDeadZone.Circular) : default;

        if (!_hasPreviousMouse)
        {
            _previousMouse = _mouse;
            _hasPreviousMouse = true;
        }

        PointerDelta = _mouse.Position - _previousMouse.Position;

        UpdateActions();
        UpdateAnalog(deltaSeconds);
        UpdateActiveDevice();
    }

    private void UpdateActions()
    {
        bool alt = _keyboard.IsKeyDown(Keys.LeftAlt) || _keyboard.IsKeyDown(Keys.RightAlt);

        foreach (GameAction action in Enum.GetValues<GameAction>())
        {
            foreach (InputBinding binding in _bindings.For(action))
            {
                bool active = binding switch
                {
                    // Alt+Enter must not also count as plain Enter (Confirm), and vice versa.
                    KeyBinding key => _keyboard.IsKeyDown(key.Key) && key.RequiresAlt == alt,
                    GamepadBinding pad => _gamepad.IsButtonDown(pad.Button),
                    _ => false,
                };

                if (active)
                {
                    _down.Add(action);
                    break;
                }
            }
        }
    }

    private void UpdateAnalog(float deltaSeconds)
    {
        // Gamepad sticks report Y up as positive; screens count Y downward, so flip it.
        Vector2 stick = _gamepad.ThumbSticks.Right;
        Vector2 pan = new(stick.X, -stick.Y);

        Vector2 keys = Vector2.Zero;
        if (_keyboard.IsKeyDown(Keys.A)) keys.X -= 1;
        if (_keyboard.IsKeyDown(Keys.D)) keys.X += 1;
        if (_keyboard.IsKeyDown(Keys.W)) keys.Y -= 1;
        if (_keyboard.IsKeyDown(Keys.S)) keys.Y += 1;
        if (keys != Vector2.Zero)
        {
            pan = Vector2.Normalize(keys);
        }
        Pan = pan.Length() < StickDeadzone ? Vector2.Zero : pan;

        // Triggers: hold for a steady zoom (scaled by frame time so it's
        // framerate-independent). Wheel: discrete steps.
        float triggers = (_gamepad.Triggers.Right - _gamepad.Triggers.Left) * deltaSeconds;
        float wheel = (_mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue) / WheelNotch;
        Zoom = triggers + wheel;
    }

    private void UpdateActiveDevice()
    {
        bool gamepadUsed = _gamepad.IsConnected && (
            _gamepad.Buttons != default(GamePadButtons)
            || _gamepad.DPad != default(GamePadDPad)
            || _gamepad.ThumbSticks.Left.LengthSquared() > StickDeadzone * StickDeadzone
            || _gamepad.ThumbSticks.Right.LengthSquared() > StickDeadzone * StickDeadzone
            || _gamepad.Triggers.Left > StickDeadzone
            || _gamepad.Triggers.Right > StickDeadzone);

        bool keyboardMouseUsed = _keyboard.GetPressedKeyCount() > 0
            || PointerDelta != Point.Zero
            || _mouse.LeftButton == ButtonState.Pressed
            || _mouse.RightButton == ButtonState.Pressed
            || _mouse.ScrollWheelValue != _previousMouse.ScrollWheelValue;

        if (gamepadUsed)
        {
            ActiveDevice = InputDevice.Gamepad;
        }
        else if (keyboardMouseUsed)
        {
            ActiveDevice = InputDevice.KeyboardMouse;
        }
    }

    // While the debug panel owns the mouse, the game still sees where the pointer
    // is (so nothing jumps when control returns) but no clicks or scrolling.
    private MouseState KeepPositionOnly(MouseState real) => new(
        real.X, real.Y, _previousMouse.ScrollWheelValue,
        ButtonState.Released, ButtonState.Released, ButtonState.Released,
        ButtonState.Released, ButtonState.Released, _previousMouse.HorizontalScrollWheelValue);
}
