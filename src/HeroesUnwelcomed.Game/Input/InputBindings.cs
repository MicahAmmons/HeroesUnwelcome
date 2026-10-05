using Microsoft.Xna.Framework.Input;

namespace HeroesUnwelcomed.Game.Input;

/// <summary>One physical way to trigger an action.</summary>
public abstract record InputBinding;

/// <summary>A keyboard key, optionally only while Alt is held (e.g. Alt+Enter).</summary>
public sealed record KeyBinding(Keys Key, bool RequiresAlt = false) : InputBinding;

/// <summary>A gamepad button (MonoGame uses Xbox names: A, B, X, Y, Start, Back...).</summary>
public sealed record GamepadBinding(Buttons Button) : InputBinding;

/// <summary>
/// Maps each <see cref="GameAction"/> to the buttons that trigger it.
/// Several bindings per action is normal: Confirm is Enter, Space AND gamepad A.
/// </summary>
/// <remarks>
/// The defaults live here for now. A later step is loading player overrides from
/// a settings file, which is all a "rebind controls" menu needs.
/// </remarks>
public sealed class InputBindings
{
    private readonly Dictionary<GameAction, List<InputBinding>> _bindings = [];

    public IReadOnlyList<InputBinding> For(GameAction action) =>
        _bindings.TryGetValue(action, out var list) ? list : [];

    public InputBindings Bind(GameAction action, params InputBinding[] bindings)
    {
        if (!_bindings.TryGetValue(action, out var list))
        {
            list = [];
            _bindings[action] = list;
        }
        list.AddRange(bindings);
        return this;
    }

    public static InputBindings CreateDefaults() => new InputBindings()
        .Bind(GameAction.Confirm, new KeyBinding(Keys.Enter), new KeyBinding(Keys.Space), new GamepadBinding(Buttons.A))
        .Bind(GameAction.Cancel, new KeyBinding(Keys.Escape), new KeyBinding(Keys.Back), new GamepadBinding(Buttons.B))
        .Bind(GameAction.NavigateUp, new KeyBinding(Keys.Up), new GamepadBinding(Buttons.DPadUp), new GamepadBinding(Buttons.LeftThumbstickUp))
        .Bind(GameAction.NavigateDown, new KeyBinding(Keys.Down), new GamepadBinding(Buttons.DPadDown), new GamepadBinding(Buttons.LeftThumbstickDown))
        .Bind(GameAction.NavigateLeft, new KeyBinding(Keys.Left), new GamepadBinding(Buttons.DPadLeft), new GamepadBinding(Buttons.LeftThumbstickLeft))
        .Bind(GameAction.NavigateRight, new KeyBinding(Keys.Right), new GamepadBinding(Buttons.DPadRight), new GamepadBinding(Buttons.LeftThumbstickRight))
        .Bind(GameAction.Pause, new KeyBinding(Keys.P), new GamepadBinding(Buttons.Start))
        .Bind(GameAction.SpeedUp, new KeyBinding(Keys.OemCloseBrackets), new KeyBinding(Keys.OemPlus), new GamepadBinding(Buttons.RightShoulder))
        .Bind(GameAction.SpeedDown, new KeyBinding(Keys.OemOpenBrackets), new KeyBinding(Keys.OemMinus), new GamepadBinding(Buttons.LeftShoulder))
        .Bind(GameAction.ToggleDoor, new KeyBinding(Keys.L), new GamepadBinding(Buttons.Y))
        .Bind(GameAction.SendParty, new KeyBinding(Keys.N), new GamepadBinding(Buttons.X))
        .Bind(GameAction.ToggleFullscreen, new KeyBinding(Keys.F11), new KeyBinding(Keys.Enter, RequiresAlt: true))
        .Bind(GameAction.Screenshot, new KeyBinding(Keys.F12))
        .Bind(GameAction.ToggleDebugPanel, new KeyBinding(Keys.F1), new KeyBinding(Keys.OemTilde), new GamepadBinding(Buttons.Back));
}
