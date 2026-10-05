using HeroesUnwelcomed.Game.Display;
using HeroesUnwelcomed.Game.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game.Scenes;

/// <summary>
/// Input playground: a grid of slots with ONE selection that keyboard, gamepad
/// and mouse all move. It's an early preview of how picking a dungeon room will
/// work. Also shows every input value live, so you can see the input layer working.
/// </summary>
public sealed class SandboxScene : IScene
{
    private const int Columns = 6;
    private const int Rows = 4;
    private static readonly Vector2 SlotSize = new(220, 124);
    private const int SlotGap = 16;

    private static readonly string[] SpeedLabels = ["1x", "2x", "3x"];

    private readonly GameServices _services;
    private readonly bool[,] _filled = new bool[Columns, Rows];
    private Point _selected = new(0, 0);
    private bool _paused;
    private int _speedIndex;

    public SandboxScene(GameServices services)
    {
        _services = services;
    }

    public void Update(GameTime gameTime)
    {
        InputService input = _services.Input;

        // Keyboard / d-pad / left stick move the selection one slot at a time.
        if (input.WasPressed(GameAction.NavigateLeft)) _selected.X--;
        if (input.WasPressed(GameAction.NavigateRight)) _selected.X++;
        if (input.WasPressed(GameAction.NavigateUp)) _selected.Y--;
        if (input.WasPressed(GameAction.NavigateDown)) _selected.Y++;
        _selected = new Point(Math.Clamp(_selected.X, 0, Columns - 1), Math.Clamp(_selected.Y, 0, Rows - 1));

        // The mouse selects whatever it hovers, but only when it actually moves or
        // clicks. A mouse resting on the desk must not fight the d-pad.
        bool pointerActive = input.PointerDelta != Point.Zero || input.PointerPrimaryPressed;
        Point? hovered = SlotUnderPointer();
        if (pointerActive && hovered is { } slot)
        {
            _selected = slot;
        }

        bool clickedSlot = input.PointerPrimaryPressed && hovered is not null;
        if (input.WasPressed(GameAction.Confirm) || clickedSlot)
        {
            _filled[_selected.X, _selected.Y] = !_filled[_selected.X, _selected.Y];
        }

        if (input.WasPressed(GameAction.Pause)) _paused = !_paused;
        if (input.WasPressed(GameAction.SpeedUp)) _speedIndex = Math.Min(_speedIndex + 1, SpeedLabels.Length - 1);
        if (input.WasPressed(GameAction.SpeedDown)) _speedIndex = Math.Max(_speedIndex - 1, 0);
    }

    public void Draw(SpriteBatch batch)
    {
        var draw = _services.Draw;
        var ui = _services.Ui;
        var input = _services.Input;

        batch.Begin(transformMatrix: ui.Transform, samplerState: SamplerState.LinearClamp);

        // Slots
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                Rectangle rect = SlotRect(x, y);
                Color fill = _filled[x, y] ? new Color(120, 40, 60) : new Color(30, 30, 42);
                draw.FillRect(batch, rect, fill);
                draw.OutlineRect(batch, rect, new Color(70, 70, 90));
                draw.TextCentered(batch, _filled[x, y] ? "ROOM" : $"{x},{y}", rect, new Color(150, 150, 170), 0.8f);
            }
        }

        // Selection highlight: thicker for controller players, who rely on it.
        int thickness = input.ActiveDevice == InputDevice.Gamepad ? 8 : 4;
        Rectangle selected = SlotRect(_selected.X, _selected.Y);
        selected.Inflate(thickness, thickness);
        draw.OutlineRect(batch, selected, Color.Gold, thickness);

        // Title and status
        Rectangle title = ui.Place(Anchor.Top, new Vector2(0, 40), new Vector2(900, 60));
        draw.TextCentered(batch, "Sandbox - input playground", title, Color.White);

        string status = _paused ? "PAUSED" : $"Speed {SpeedLabels[_speedIndex]}";
        Rectangle statusRect = ui.Place(Anchor.TopRight, new Vector2(40, 40), new Vector2(300, 60));
        draw.FillRect(batch, statusRect, _paused ? new Color(140, 30, 30) : new Color(30, 90, 50));
        draw.TextCentered(batch, status, statusRect, Color.White);

        DrawInputReadout(batch);
        DrawHelp(batch);

        batch.End();
    }

    private void DrawInputReadout(SpriteBatch batch)
    {
        var input = _services.Input;
        Rectangle panel = _services.Ui.Place(Anchor.TopLeft, new Vector2(40, 40), new Vector2(560, 250));
        _services.Draw.FillRect(batch, panel, new Color(0, 0, 0, 160));

        string held = string.Join(", ", Enum.GetValues<GameAction>().Where(input.IsDown));
        string[] lines =
        [
            $"Device: {input.ActiveDevice}",
            $"Selected slot: {_selected.X},{_selected.Y}",
            $"Pan: {input.Pan.X:0.00}, {input.Pan.Y:0.00}",
            $"Zoom: {input.Zoom:+0.00;-0.00;0.00}",
            $"Held: {(held.Length == 0 ? "-" : held)}",
        ];

        for (int i = 0; i < lines.Length; i++)
        {
            _services.Draw.Text(batch, lines[i], new Vector2(panel.X + 16, panel.Y + 12 + i * 46), Color.White, 0.75f);
        }
    }

    private void DrawHelp(SpriteBatch batch)
    {
        bool pad = _services.Input.ActiveDevice == InputDevice.Gamepad;
        string help = pad
            ? "D-pad: move   A: place/remove   Start: pause   LB/RB: speed   Back: dev panel"
            : "Arrows/mouse: move   Enter/click: place/remove   P: pause   [ ]: speed   F1: dev panel   F11: fullscreen";

        Rectangle rect = _services.Ui.Place(Anchor.Bottom, new Vector2(0, 40), new Vector2(1800, 50));
        _services.Draw.TextCentered(batch, help, rect, new Color(180, 180, 200), 0.7f);
    }

    private Rectangle SlotRect(int x, int y)
    {
        Vector2 gridSize = new(
            Columns * SlotSize.X + (Columns - 1) * SlotGap,
            Rows * SlotSize.Y + (Rows - 1) * SlotGap);

        // Centre the whole grid on the canvas, a little below middle.
        Rectangle grid = _services.Ui.Place(Anchor.Center, new Vector2(0, 60), gridSize);
        return new Rectangle(
            grid.X + x * (int)(SlotSize.X + SlotGap),
            grid.Y + y * (int)(SlotSize.Y + SlotGap),
            (int)SlotSize.X,
            (int)SlotSize.Y);
    }

    private Point? SlotUnderPointer()
    {
        Vector2 pointer = _services.Ui.ScreenToUi(_services.Input.PointerPosition);
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                if (SlotRect(x, y).Contains(pointer))
                {
                    return new Point(x, y);
                }
            }
        }
        return null;
    }

    public void Dispose()
    {
    }
}
