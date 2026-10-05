using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Core.Simulation;

namespace HeroesUnwelcomed.Game.Play;

/// <summary>One choice in a menu. <paramref name="Available"/> only affects how it looks: choosing an unavailable item still asks the game, which explains why not.</summary>
public sealed record MenuItem(string Label, string Detail, bool Available, Action Choose);

/// <summary>An open menu: a title and a list with one selected item.</summary>
public sealed class Menu(string title, IReadOnlyList<MenuItem> items)
{
    public string Title { get; } = title;
    public IReadOnlyList<MenuItem> Items { get; } = items;
    public int Selected { get; set; }
}

/// <summary>
/// The player's dungeon-master controls: which grid cell is selected, and the
/// build / room menus.
/// </summary>
/// <remarks>
/// <para>
/// This class takes <b>intentions</b>, not buttons: <see cref="Move"/>,
/// <see cref="PointAt"/>, <see cref="Confirm"/>, <see cref="Cancel"/>. The play
/// scene translates keyboard, gamepad and mouse into these; a touch screen
/// later will translate taps into the same calls. Nothing here depends on
/// hovering, so every action works on a controller.
/// </para>
/// <para>
/// Every change to the dungeon goes through a Core command, so the rules
/// (door must be locked, enough souls, empty cell...) are enforced in one
/// place, and the refusal reason is shown to the player as-is.
/// </para>
/// </remarks>
public sealed class BuildController
{
    private readonly Run _run;

    public BuildController(Run run)
    {
        _run = run;
        Selected = run.Dungeon.Rooms.Count > 1 ? run.Dungeon.Rooms[1].Position : run.Dungeon.Entrance.Position;
    }

    public GridPoint Selected { get; private set; }
    public Menu? OpenMenu { get; private set; }

    /// <summary>Feedback for the player (results and refusal reasons).</summary>
    public event Action<string>? Message;

    public Room? SelectedRoom => _run.Dungeon.RoomAt(Selected);

    /// <summary>D-pad / arrows: move the menu selection if a menu is open, otherwise the grid selection.</summary>
    public void Move(int dx, int dy)
    {
        if (OpenMenu is { } menu)
        {
            if (dy != 0 && menu.Items.Count > 0)
            {
                menu.Selected = ((menu.Selected + dy) % menu.Items.Count + menu.Items.Count) % menu.Items.Count;
            }
            return;
        }

        Selected = new GridPoint(
            Math.Clamp(Selected.X + dx, 0, _run.Dungeon.Width - 1),
            Math.Clamp(Selected.Y + dy, 0, _run.Dungeon.Height - 1));
    }

    /// <summary>Pointer moved over a cell (mouse now, touch later). Ignored while a menu is open.</summary>
    public void PointAt(GridPoint cell)
    {
        if (OpenMenu is null && _run.Dungeon.InBounds(cell))
        {
            Selected = cell;
        }
    }

    /// <summary>Pointer moved over a menu item.</summary>
    public void HighlightMenuItem(int index)
    {
        if (OpenMenu is { } menu && index >= 0 && index < menu.Items.Count)
        {
            menu.Selected = index;
        }
    }

    /// <summary>A / Enter / click: choose the menu item, or open the menu for the selected cell.</summary>
    public void Confirm()
    {
        if (OpenMenu is { } menu)
        {
            if (menu.Items.Count > 0)
            {
                menu.Items[menu.Selected].Choose();
            }
            return;
        }
        OpenMenuForSelection();
    }

    /// <summary>B / Esc: close the menu.</summary>
    public void Cancel() => OpenMenu = null;

    private void OpenMenuForSelection()
    {
        if (_run.IsOver)
        {
            return;
        }

        if (SelectedRoom is { } room)
        {
            OpenMenu = RoomMenu(room);
        }
        else if (_run.Door != DoorState.Locked)
        {
            Say("Lock the door to build here (L / Y). Building is only allowed while no heroes are inside.");
        }
        else
        {
            OpenMenu = BuildMenu(Selected);
        }
    }

    private Menu BuildMenu(GridPoint cell)
    {
        var items = _run.Definitions.RoomKinds
            .Where(k => k.Buildable)
            .OrderBy(k => k.Category).ThenBy(k => k.PlaceCost)
            .Select(kind => new MenuItem(
                kind.Id,
                $"{kind.PlaceCost} souls · {kind.Category} · difficulty {kind.Difficulty:0.0}",
                _run.Souls.CanAfford(kind.PlaceCost),
                () =>
                {
                    CommandResult result = _run.Execute(new PlaceRoom(kind.Id, cell));
                    Say(result.Accepted ? $"Built {kind.Id}. Heroes will reach it after the rooms built before it." : result.Reason!);
                    if (result.Accepted) OpenMenu = null;
                }))
            .ToList();

        items.Add(new MenuItem("Cancel", "", true, Cancel));
        return new Menu($"Build at {cell}", items);
    }

    private Menu RoomMenu(Room room)
    {
        bool locked = _run.Door == DoorState.Locked;
        int order = _run.Dungeon.IndexOf(room);
        var items = new List<MenuItem>();

        if (room.Kind.IsChallenge)
        {
            bool maxed = room.Level >= room.Kind.MaxLevel;
            int cost = room.Kind.UpgradeCostFrom(room.Level);
            items.Add(new MenuItem(
                "Upgrade",
                maxed ? "Already at maximum level" : $"Level {room.Level} → {room.Level + 1} · {cost} souls · difficulty {room.Kind.DifficultyAt(room.Level + 1):0.0}",
                locked && !maxed && _run.Souls.CanAfford(cost),
                () =>
                {
                    CommandResult result = _run.Execute(new UpgradeRoom(room.Id));
                    Say(result.Accepted ? $"{room.Kind.Id} is now level {room.Level}." : result.Reason!);
                    if (result.Accepted) OpenMenu = RoomMenu(room); // refresh the numbers
                }));
        }

        items.Add(new MenuItem(
            "Flip direction",
            $"Heroes travel {(room.Facing == Facing.Right ? "left" : "right")} through it instead",
            locked,
            () =>
            {
                CommandResult result = _run.Execute(new FlipRoom(room.Id));
                Say(result.Accepted ? $"{room.Kind.Id} now faces {room.Facing.ToString().ToLowerInvariant()}." : result.Reason!);
                if (result.Accepted) OpenMenu = RoomMenu(room);
            }));

        items.Add(new MenuItem("Close", "", true, Cancel));
        string title = order == 0 ? "Entrance" : $"#{order} {room.Kind.Id}" + (room.Kind.IsChallenge ? $" (level {room.Level})" : "");
        return new Menu(title, items);
    }

    private void Say(string message) => Message?.Invoke(message);
}
