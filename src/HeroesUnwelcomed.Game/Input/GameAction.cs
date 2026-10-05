namespace HeroesUnwelcomed.Game.Input;

/// <summary>
/// Everything the player can *do*, independent of which button does it.
/// </summary>
/// <remarks>
/// Game code asks "was Confirm pressed?", never "was Enter or the A button
/// pressed?". Which physical buttons trigger each action is decided in one place,
/// <see cref="InputBindings"/>. That's what lets keyboard, gamepad and (later)
/// touch all drive the same game, and lets players rebind controls.
/// </remarks>
public enum GameAction
{
    // Menus and selection
    Confirm,
    Cancel,
    NavigateUp,
    NavigateDown,
    NavigateLeft,
    NavigateRight,

    // Game flow
    Pause,
    SpeedUp,
    SpeedDown,
    ToggleDoor,
    SendParty,

    // System
    ToggleFullscreen,
    Screenshot,

    // Developer only (ignored in Release builds)
    ToggleDebugPanel,
}
